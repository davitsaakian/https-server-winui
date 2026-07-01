# LocalServer Code Documentation

`LocalServer` implements a small HTTPS server inside the WinUI app. It listens on loopback, accepts TLS connections with the bundled local certificate, parses simple HTTP/1.1 requests, routes them to registered controllers, and returns raw HTTP responses.

The server currently exposes a temporary file resource API:

```text
POST https://localhost:4000/storage
GET  https://localhost:4000/storage?file={fileId}
```

## Folder Layout

```text
LocalServer/
  Constants/
    LocalServerConstants.cs
  Core/
    Controllers/
    Managers/
    Middleware/
    Models/
    Services/
    HttpsServer.cs
    TcpServer.cs
  Managers/
    IServerFileStorageManager.cs
    ServerFileStorageManager.cs
  ResourcesLocalServer/
    Controllers/
    Middleware/
```

## Startup Flow

The app starts the local server from `App.xaml.cs`:

1. `App.ConfigureServices()` registers the server services through `UseMiniAppsSdkServices()`.
2. `App.InitializeComponents()` loads `Assets/HttpsCertificates/localhostCertificate.pfx`.
3. `ServerInitializationService.Initialize()` converts that file into an `X509Certificate2`.
4. `ServerInitializationService.StartServer()` stores the certificate in `HttpsCertificateManager`, clears temporary server files, and starts `HttpsServer` on `127.0.0.1:4000`.

The server is started fire-and-forget with `_ = _resourcesServer.Start(serverAddress, serverPort);`, so app launch continues while the listener runs.

## Constants

`Constants/LocalServerConstants.cs` defines the public shape of the local API:

| Constant | Value | Purpose |
| --- | --- | --- |
| `LOCAL_ADDRESS` | `IPAddress.Loopback` | Binds the server to local loopback only. |
| `PORT` | `4000` | HTTPS listener port. |
| `STORAGE_ROUTE` | `"/storage"` | Route used by the storage resource controllers. |
| `FILE_ID_QUERY_NAME` | `"file"` | Query-string key for retrieving a stored file. |
| `FULL_ADDRESS` | `https://localhost:4000/storage?file=` | Convenience URL prefix for storage reads. |

## Core Server

### `TcpServer`

`Core/TcpServer.cs` is the transport base class. It owns:

- `TcpListener` setup and shutdown.
- The accept loop for incoming clients.
- Per-client stream handling.
- Basic exception logging for unhandled client errors.

Subclasses implement `HandleClientStreamAsync(NetworkStream stream)` to decide what protocol to speak over the TCP stream.

### `HttpsServer`

`Core/HttpsServer.cs` extends `TcpServer` and implements the HTTPS/HTTP layer:

1. `OnStarting()` loads the certificate for the requested IP address from `HttpsCertificateManager`.
2. Each TCP stream is wrapped in `SslStream`.
3. `AuthenticateAsServerAsync()` performs a TLS 1.2 server handshake.
4. `RequestModelCreator` parses the HTTP request.
5. Request middleware runs.
6. The matching controller handles the request.
7. Response middleware runs.
8. `ResponseStringCreator` serializes the response back to bytes.

Request processing has a 30 second timeout. SSL handshake failures, request parsing failures, and response write failures are logged with `Debug.WriteLine()`.

Controller matching uses:

- Exact HTTP method equality.
- `request.LocalUrl.StartsWith(controller.Url, StringComparison.OrdinalIgnoreCase)`.

Because URL matching is prefix-based, more specific routes should be registered before broader routes when that matters.

## Certificates

`Core/Managers/HttpsCertificateManager.cs` is a singleton certificate registry keyed by `IPAddress`.

It stores:

- The `X509Certificate2` used by `HttpsServer`.
- The certificate serial number converted to Base64.

`WebView/MessagingWebView.cs` uses `CompareDerEncodedSerialNumber()` during `ServerCertificateErrorDetected`. If the WebView sees the same local certificate that was registered for loopback, it sets the WebView action to `AlwaysAllow`.

## Request Parsing

`Core/Services/RequestModelCreator.cs` parses a minimal HTTP request:

- Reads the request line, for example `GET /storage?file=test.png HTTP/1.1`.
- Extracts the HTTP method and local URL.
- Parses query parameters with `HttpUtility.ParseQueryString()`.
- Reads headers until an empty line.
- Reads the request body only when `Content-Length` is present and positive.

The maximum accepted body size is `500 MB`. If `Content-Length` is larger than that, parsing throws and the server returns a `500 Server Error`.

The parser does not implement chunked transfer encoding.

## Response Writing

`Core/Services/ResponseStringCreator.cs` builds raw HTTP/1.1 response bytes:

1. Status line: `HTTP/1.1 {StatusCode} {StatusMessage}`
2. Headers
3. Blank line
4. Optional binary body

Response bodies are appended as bytes after the UTF-8 encoded status line and headers.

## Models

### `RequestModel`

`Core/Models/RequestModel.cs` stores:

- `Method`
- `LocalUrl`
- `QueryParameters`
- `Headers`
- `Body`

`GetFullUrl(string baseUrl)` is used for diagnostic logging.

### `ResponseModel`

`Core/Models/Responses/ResponseModel.cs` stores:

- `StatusCode`
- `StatusMessage`
- `Headers`
- `Body`

Convenience response types exist for common statuses:

| Class | Status |
| --- | --- |
| `SuccessResponse` | `200 OK` |
| `BadRequestResponse` | `400 Bad Request` |
| `NotFoundResponse` | `404 Not Found` |
| `ServerErrorResponse` | `500 Server Error` |

Some controllers also create custom `ResponseModel` instances, such as `415 Unsupported Media Type`.

## Middleware

There are two middleware interfaces:

- `IServerRequestMiddleware` handles parsed requests before controller routing.
- `IServerResponseMiddleware` handles responses before they are serialized.

`HttpsServer` includes two response middleware by default:

- `DateHeaderMiddleware` adds a UTC `Date` header when missing.
- `ContentLengthHeaderMiddleware` adds `Content-Length`, using `0` when the body is null.

`ServerInitializationService` also registers `CorsMiddleware`, which adds:

- `Access-Control-Allow-Origin: *`
- `Access-Control-Allow-Methods: GET, POST, OPTIONS`
- `Access-Control-Allow-Headers: *`
- `Access-Control-Allow-Credentials: true`

`AddMiddleware()` inserts middleware at the beginning of the relevant list. Middleware added later runs earlier.

## Controllers

All controllers inherit from `Core/Controllers/ServerController.cs`.

Each controller defines:

- `Method`
- `Url`
- `HandleRequest(RequestModel request)`

`ServerController` also provides `IsValidFileId()`, which rejects empty file IDs, invalid filename characters, and `..` path traversal.

### `PreflightController`

`ResourcesLocalServer/Controllers/PreflightController.cs`

- Method: `OPTIONS`
- URL: `/`
- Response: `200 OK`

This handles CORS preflight requests. Because controller URL matching is prefix-based and every path starts with `/`, this controller can answer `OPTIONS` requests for any route.

### `StorageResourceMultipartDataPostController`

`ResourcesLocalServer/Controllers/StorageResourceMultipartDataPostController.cs`

- Method: `POST`
- URL: `/storage`
- Expected content type: `multipart/form-data`
- Response: `200 OK` on success
- Response: `415 Unsupported Media Type` when the content type is not multipart form data

This controller parses the request body with `HttpMultipartParser`. For each uploaded file:

1. The multipart file name is treated as the storage ID.
2. Invalid file IDs are skipped.
3. The file stream is copied into a byte array.
4. The bytes are written through `IServerFileStorageManager`.

### `StorageResourceGetController`

`ResourcesLocalServer/Controllers/StorageResourceGetController.cs`

- Method: `GET`
- URL: `/storage`
- Query: `file={fileId}`

Behavior:

1. Reads the `file` query parameter.
2. Validates it with `IsValidFileId()`.
3. Reads the matching file from temporary storage.
4. Returns `400 Bad Request` for an invalid file ID.
5. Returns `404 Not Found` when the file does not exist.
6. Returns `200 OK` with the file bytes and a `Content-Type` inferred by `MimeMapping`.

## File Storage

`Managers/ServerFileStorageManager.cs` stores server resources under:

```text
ApplicationData.Current.TemporaryFolder/ServerResources
```

It supports:

- Deleting all stored files during server initialization.
- Writing bytes by ID.
- Writing a `IRandomAccessStream` by ID.
- Reading bytes by ID.
- Returning the local file path for an ID.

Writes are protected with a per-file `SemaphoreSlim` stored in a `ConcurrentDictionary`, so concurrent writes to the same file ID are serialized.

## Dependency Injection

`DependencyInjection/MiniAppsSdkServiceCollectionExtensions.cs` registers LocalServer services:

| Service | Lifetime |
| --- | --- |
| `IServerFileStorageManager` / `ServerFileStorageManager` | Singleton |
| `StorageResourceGetController` | Singleton |
| `StorageResourceMultipartDataPostController` | Singleton |
| `PreflightController` | Singleton |
| `CorsMiddleware` | Singleton |
| `HttpsServer` | Singleton |
| `IServerInitializationService` / `ServerInitializationService` | Transient |

`ServerInitializationService` wires controllers and middleware into the singleton `HttpsServer`.

## Adding a New Endpoint

To add another local endpoint:

1. Create a controller that inherits `ServerController`.
2. Set `Method` and `Url`.
3. Implement `HandleRequest()`.
4. Register the controller in `UseMiniAppsSdkServices()`.
5. Add it to `_resourcesServer` inside `ServerInitializationService`.

Example shape:

```csharp
internal class ExampleController : ServerController
{
    public override HttpMethod Method => HttpMethod.Get;
    public override string Url => "/example";

    public override Task<ResponseModel> HandleRequest(RequestModel request)
    {
        return Task.FromResult<ResponseModel>(new SuccessResponse());
    }
}
```

## Current Limitations

- Request parsing is intentionally small and does not support chunked request bodies.
- Route matching is prefix-based.
- The body size limit is enforced from `Content-Length`.
- The server is bound to loopback and intended for local app/WebView use, not general network hosting.
- `CorsMiddleware` currently allows all origins.
