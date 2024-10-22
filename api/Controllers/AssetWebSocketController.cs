using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using app_core.dto;
using app_core.service;
using DotNext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("streaming/asset")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AssetWebSocketController : ControllerBase
    {
        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
        };

        [Route("{id}/{provider}/realtime")]
        public async Task GetRealTimePrices(
            [FromServices] AssetService assetService,
            [FromServices] IAssetRealtimePriceProvider realtimePriceProvider,
            [FromRoute] Guid id,
            [FromRoute] string provider,
            [FromQuery] List<RealtimePriceKind> kinds
        )
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                await HandleWebsocketConnection(
                    assetService,
                    realtimePriceProvider,
                    id,
                    provider,
                    kinds
                );
            }
            else
            {
                BadRequest("Not a websocket request");
            }
        }

        private async Task HandleWebsocketConnection(
            AssetService assetService,
            IAssetRealtimePriceProvider realtimePriceProvider,
            Guid id,
            string provider,
            List<RealtimePriceKind> kinds
        )
        {
            var assetResult = await assetService.GetAssetByIdAndProvider(
                id,
                new Provider(provider)
            );
            if (!assetResult.TryGet(out var asset))
            {
                BadRequest(assetResult.Error!);
                return;
            }

            var source = new CancellationTokenSource();
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            if (kinds.Count == 0)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.InvalidPayloadData,
                    "At least one kind is required",
                    CancellationToken.None
                );
            }
            Task.Run(async () => await ListenToDisconnect(source, webSocket));

            var realtimeEnumerator = realtimePriceProvider
                .GetAssetRealtimePrices(asset, new Provider(provider), kinds, source.Token)
                .GetAsyncEnumerator(source.Token);

            while (await realtimeEnumerator.MoveNextAsync())
            {
                await SendRealtimePrice(source, webSocket, realtimeEnumerator.Current);
            }
        }

        private async Task SendRealtimePrice(
            CancellationTokenSource source,
            WebSocket webSocket,
            Result<AssetRealtimePrice> realtimePrice
        )
        {
            if (!realtimePrice.TryGet(out var price))
            {
                source.Cancel();
                await webSocket.CloseOutputAsync(
                    WebSocketCloseStatus.InternalServerError,
                    realtimePrice.Error!.Message,
                    CancellationToken.None
                );
            }
            await webSocket.SendAsync(
                new ArraySegment<byte>(
                    Encoding.ASCII.GetBytes(JsonSerializer.Serialize(price, jsonOptions)!)
                ),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        private static async Task ListenToDisconnect(
            CancellationTokenSource source,
            WebSocket webSocket
        )
        {
            var buffer = MemoryPool<byte>.Shared.Rent(1024);
            while (!source.Token.IsCancellationRequested)
            {
                var message = await webSocket.ReceiveAsync(buffer.Memory, source.Token);
                if (message.MessageType == WebSocketMessageType.Close)
                {
                    source.Cancel();
                    await webSocket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Ended due to disconnect",
                        CancellationToken.None
                    );
                    break;
                }
            }
        }
    }
}
