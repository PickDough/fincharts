# Real-Time Asset Price Streaming App
=====================================

Overview
--------

A .NET Core app that provides assets realtime as well as historical prices.

### Components

* API: Lists assets, outputs asset's price over time, streams realtime price via a websocket
* Assets Fetcher: Fetches asset list from external providers and stores them in a database
* Database: Stores available assets

### Running the App

```bash
docker-compose up
```

### Testing the App

Import Rest requests from [Postman collection](./Fintacharts%20Test%20Case.postman_collection.json).

You can also test it in the terminal:

#### Rest Api

```bash
# List Assets
curl -X GET "http://localhost:8080/api/assets?page=1&perPage=10"
```

```bash
# Historical price
curl -X GET "http://localhost:8080/api/Assets/054dc5aa-7d4e-45b5-abea-11cb24823ce4/oanda/count-back?interval=1&periodicity=Minute&count=10" -H "accept: */*"
```

#### WebSocket

```bash
# https://github.com/vi/websocat
websocat "ws://localhost:8080/streaming/asset/054dc5aa-7d4e-45b5-abea-11cb24823ce4/oanda/realtime?kinds=bid&kinds=ask"
```