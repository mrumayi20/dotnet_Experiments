# Authentication and Authorization

## The Authentication Flow

When a request hits your API, it passes through various "checkpoints" (Middleware). <br>

1. Request enters: It carries a token or cookie.

2. Authentication Middleware: It looks for that token. If found and valid, it creates a ClaimsPrincipal (a "User" object) and attaches it to the request.

3. Authorization Middleware: It checks if the "User" object has the right permissions for the specific endpoint.

## Does authentication and authorization always go together?

Yes most of the time.

## Can you have one without the other?

<b>Authentication without Authorization:</b> Possible, but rare. It’s like a "Members Only" club where once you're inside, everyone has access to everything.

<b>Authorization without Authentication:</b> Impossible in a secure system. You cannot determine if someone is "allowed" to do something if you haven't identified who they are first.

## The Middleware "Hand-off"

In your .NET code, they are separate steps in the pipeline.

1. app.UseAuthentication(): This looks at the incoming request (the token or cookie). It populates the User object (technically called ClaimsPrincipal).

2. app.UseAuthorization(): This looks at that User object and checks if it meets the requirements (e.g., [Authorize(Roles = "Admin")]).

## Types of Authentication

1. API KEY Authentication
   This involves the client sending a "Secret Key" in the Request Header, and your server validating it. Implemented in ApiKeyMiddleware.cs.
   Tests are written in Authentication.http

<b>Test using POSTMAN</b>

```
1. Set the method to GET.

2. Enter the URL: http://localhost:xxxx/weatherforecast.
(replace xxxx with the port number your server is running on)

3. Go to the Headers tab.

4. In the Key column, type: APIKEYNAME.

5. In the Value column, type: MySecret_123.

6. Click Send.
```
