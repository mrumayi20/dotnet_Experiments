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

## 1. API KEY Authentication

```
1. Set the method to GET.

2. Enter the URL: http://localhost:xxxx/weatherforecast.
(replace xxxx with the port number your server is running on)

3. Go to the Headers tab.

4. In the Key column, type: APIKEYNAME.

5. In the Value column, type: MySecret_123.

6. Click Send.
```

## 2. Basic Authentication

The UserID and Password aren't sent as separate parameters in the URL; they are combined, encoded, and sent in a specific header called Authorization.

<b> How it works </b></br>

```
1. The Header: The client sends:
Authorization: Basic dXNlcjpwYXNz

2. The Decoding: dXNlcjpwYXNz is just Base64. If you decode it, it reveals username:password.

3. The Comparison: You check those against your DB.

4. The Flaw: Because the password is sent with every single request, if someone sniffs the network, they have the user's actual password forever.
```

## 3. JWT Token

JWT stands for JSON Web Token. Think of it as a Digital Passport.<br>

1. You show your ID (Username/Password) to the server.<br>
2. The server gives you a signed Passport (Token).<br>
3. You show this Passport every time you want to enter a "Protected" area.<br>

<b>The Flow of this Project</b><br>

1. The Login (/login): This is where the token is born. We create "Claims" (info about the user), sign it with a Secret Key, and send the string back to the user.
2. The Rules (AddJwtBearer): We tell the app exactly what a "Valid" passport looks like (Must have the right Issuer, Audience, and the correct Secret Signature).

3. The Bouncer (UseAuthentication): This middleware intercepts every request. It looks for a token. If it finds one, it verifies it using the "Rules" we defined.

4. The Gate (RequireAuthorization): This lock is placed on specific endpoints. If the Bouncer hasn't verified you, this lock won't open.

<b>Why use .RequireAuthorization() instead of [Authorize]?</b><br>
.RequireAuthorization(): Used for Minimal APIs (the app.MapGet style).<br>
[Authorize] attribute: Used in Controller-based APIs (where you have separate classes for Controllers).<br>

Both do the exact same thing: They trigger the Authorization middleware to check if the user is identified.<br>

<b>How to test this experiment</b></br>

1. Get the Token: Call GET /login.
   Copy the long string result.

2. Access Protected: Call GET /protected.
   It will fail with 401 Unauthorized at first.

3. Use the Passport: Call GET /protected again, but add a Header:
   Key: Authorization
   Value: Bearer <your_token_here>

   ![Authentication Demo](./screenshots/JWT_Auth.gif)

# Authorization

## Types of Authrization

## 1. Simple Authorization

This is the most basic level. It simply checks if a user is authenticated (logged in) without caring about their specific permissions.<br>
<b>How to use:</b> Apply the [Authorize] attribute to a controller or action.<br>
Implemented in `Program.cs` by configuring JWT Bearer authentication and protecting the /protected endpoint. ` .RequireAuthorization();`

## 2. Role-Based Authorization (RBAC)

This checks if a user belongs to a specific group or "Role" (e.g., Admin, Moderator, User).<br>
<b>How to use:</b> Use [Authorize(Roles = "Admin")]. You can also allow multiple roles using a comma-separated list: [Authorize(Roles = "Admin, Manager")].<br>
<b>Limitations:</b> It can lead to "role explosion" where you have to create dozens of roles for every tiny variation in permission.<br>
This is how roles are defined.

```
// Inside your Login Method
var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.Email, user.Email),

    // THIS is where you define the Role
    new Claim(ClaimTypes.Role, "Admin"),

    // THIS is where you define a custom Claim
    new Claim("EmployeeId", "12345")
};

// These claims are then encoded into your JWT Token or Cookie
```

## 3. Claims-Based Authorization

A "Claim" is a piece of information about the user (e.g., Date of Birth, Employee ID, or Email). You can authorize users based on these specific attributes rather than just a broad role name.<br>
<b>Example:</b> Only allowing users who have an "EmployeeNumber" claim.<br>

Implemented by adding `new Claim(ClaimTypes.Name, "YourName")` to the JWT payload in the `/login` endpoint. The token itself acts as a claims-based identity.

## 4. Policy-Based Authorization (The Modern Standard)

This is the most flexible and recommended approach. A Policy is a named requirement that can combine roles, claims, and custom logic.<br>
<b>How it works:</b> You define a policy in Program.cs (e.g., a "MinimumAge" policy) and then apply it using [Authorize(Policy = "AtLeast18")].<br>
<b>Components:</b> It consists of a Requirement (what is needed) and a Handler (the code that checks if the user meets that need).<br>

This is how policies are defined

```
// Inside Program.cs
builder.Services.AddAuthorization(options =>
{
    // You are defining a Policy named "SuperUser"
    options.AddPolicy("SuperUser", policy =>
        policy.RequireRole("Admin")
              .RequireClaim("EmployeeId"));
});
```

## 5. Resource-Based Authorization

Sometimes, you can't decide if a user is authorized until you see the specific data they are trying to access.<br>
<b>Scenario:</b> A user is authorized to "Edit Documents," but they should only be able to edit their own documents, not someone else's.<br>
<b>Implementation:</b> Since the [Authorize] attribute runs before the data is loaded, you must perform this check imperatively (using code) inside your controller action using IAuthorizationService.<br>

```
app.MapPost("/documents/{id}/edit", async (int id, HttpContext context, IDocumentService documentService) =>
{
    // 1. Fetch the resource from your database
    var document = await documentService.GetById(id);
    if (document == null) return Results.NotFound();

    // 2. Get the Current User's ID from their Claims (Passport)
    var currentUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // 3. The "Manual" Authorization Check
    // We check if the user is the owner OR if they are an Admin
    if (document.OwnerId != currentUserId && !context.User.IsInRole("Admin"))
    {
        // Return 403 Forbidden because they are logged in,
        // but don't own THIS specific piece of data.
        return Results.Forbid();
    }

    // 4. If they pass the check, proceed with the edit
    return Results.Ok("You are authorized to edit this specific document.");
}).RequireAuthorization(); // Requires a valid token first
```

| Type               | Best For                                     | Complexity |
| :----------------- | :------------------------------------------- | :--------- |
| **Simple**         | Any logged-in user                           | Very Low   |
| **Role-Based**     | Broad categories (Admin vs. User)            | Low        |
| **Claims-Based**   | Specific user attributes (Email, ID)         | Medium     |
| **Policy-Based**   | Complex rules (Rules + Claims + Logic)       | High       |
| **Resource-Based** | Ownership (e.g., "Only the author can edit") | High       |

## How do you handle authorization?

I usually start with Simple Authorization for basic access. For grouping users, I use Role-Based access, but for anything complex, I prefer Policy-Based Authorization because it's more maintainable and allows us to combine roles and claims into a single requirement. Finally, for data-specific security—like ensuring a user only edits their own profile—I implement Resource-Based Authorization using the `IAuthorizationService`.
