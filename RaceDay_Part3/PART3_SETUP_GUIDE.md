# RaceDay Part 3 - Setup Guide

## What's new in this zip

Your Part 2 API folder is still here, untouched except for two additions
(see below). The new piece is the MVC front end:

```
RaceDay.sln                     <- now has 3 projects: Api, Api.Tests, Mvc
RaceDay.Api/                    <- same Part 2 API, PLUS:
  Services/BlobStorageService.cs    <- uploads images to Azure Blob Storage
  Controllers/FilesController.cs    <- POST /api/files/upload (used by the MVC app)
  (Event and User models now have BannerImageUrl / ProfilePictureUrl)
RaceDay.Mvc/                    <- the new web front end
  Controllers/                  <- Account, Events, Categories, Enrolments, Results, Profile, Home
  Views/                        <- one folder per controller, plus Shared/_Layout.cshtml
  Services/ApiClient.cs         <- every page talks to the API through this, never the database
  Dockerfile                    <- builds and runs the MVC app in a container
.github/workflows/dotnet-ci.yml <- now builds + tests all 3 projects, on branch "part3" too
```

How it fits together: the MVC app has **no database connection of its own**.
Every page (login, events list, enrol, results, profile) calls the Part 2 API
over HTTP using `ApiClient`, the same way Swagger did. Login sends your
credentials to `/api/auth/login`, and the MVC app keeps the API's auth cookie
in its own session so every later request is already logged in. Organiser and
Participant each see a different menu and different pages — enforced both by
hiding links in the layout and by `[RequireRole]` filters on the MVC
controllers themselves (not just hiding buttons).

**Important, same as before: I could not compile this myself** — no .NET SDK
in this sandbox. I reviewed every file by hand and cross-checked that
property names match between the API's DTOs and the MVC app's copies of
them, but Visual Studio's compiler is still the real test. If anything shows
a red squiggly or fails to build, paste me the exact error text.

## Step 1: Open it in Visual Studio

1. Unzip this into a folder.
2. Double-click `RaceDay.sln` — it now opens **three** projects: `RaceDay.Api`,
   `RaceDay.Api.Tests`, and `RaceDay.Mvc`.
3. Let NuGet restore automatically, or right-click the solution →
   **Restore NuGet Packages**.

## Step 2: Database — one new migration

If you already ran `Add-Migration InitialCreate` for Part 2, the `Event` and
`User` tables are missing two new columns (`BannerImageUrl`,
`ProfilePictureUrl`). In **Package Manager Console** (default project =
`RaceDay.Api`), run:

```
Add-Migration AddImageUrls
Update-Database
```

If you're setting the database up fresh, just run `Add-Migration InitialCreate`
and `Update-Database` as before — the new columns are already included.

## Step 3: Azure Blob Storage

The brief asks for real Azure Blob Storage integration. You have two options:

**Option A — quick local testing with Azurite (no Azure account needed):**
The connection string in `RaceDay.Api/appsettings.json` already defaults to
`"UseDevelopmentStorage=true"`, which talks to Azurite, a local storage
emulator. Install it once with `npm install -g azurite` (Node.js required),
then run `azurite` in a terminal before starting the API. Uploaded images
will work exactly like real Azure, just stored locally.

**Option B — a real Azure Storage account (what your video should show,
since the brief says "Azure Blob Storage"):**
1. In the [Azure Portal](https://portal.azure.com), create a free Storage
   Account (the free tier covers this easily).
2. Go to **Access keys** on that storage account, copy a **Connection string**.
3. In Visual Studio, right-click `RaceDay.Api` → **Manage User Secrets**, and
   paste:
   ```json
   {
     "AzureBlobStorage": {
       "ConnectionString": "<paste your real connection string here>"
     }
   }
   ```
   (User Secrets keeps this out of `appsettings.json` and out of GitHub —
   never commit a real Azure connection string.)
4. Run the app — uploaded banners/profile pictures now go to real Azure, and
   you can show the container in the Azure Portal in your video.

## Step 4: Run both projects together

Right-click the **solution** → **Configure Startup Projects** → **Multiple
startup projects** → set both `RaceDay.Api` and `RaceDay.Mvc` to **Start**.
Press F5. Two browser tabs open: the API's Swagger page, and the MVC site's
home page.

Check `RaceDay.Mvc/appsettings.json` → `"ApiBaseUrl"` matches the port your
API actually started on (Visual Studio sometimes picks a different port each
run — check the API's console window title or its launchSettings.json).

Try it in this order in the MVC site:
1. Register as an Organiser, log in.
2. Create an event, upload a banner image.
3. Add a category to it.
4. Log out, register as a Participant, log in.
5. Browse events, enrol in the one you created.
6. Log back in as the Organiser, go to that event's enrolments, capture a
   result for the Participant.
7. Log back in as the Participant, check **My Results**.

## Step 5: Run the unit tests

**Test → Run All Tests.** These still run against an in-memory database and
don't touch Azure or your real SQL Server.

## Step 6: Docker

The brief only requires the **MVC app** to run in Docker (it talks to the API
over HTTP, so the API can keep running normally in Visual Studio while the
MVC app runs containerised). From the solution's root folder (the one
containing `RaceDay.sln`), run:

```
docker build -t raceday-mvc -f RaceDay.Mvc/Dockerfile .
docker run -p 8080:8080 -e ApiBaseUrl=http://host.docker.internal:5000 raceday-mvc
```

Then open `http://localhost:8080` — replace `5000` with whatever port your
API is actually running on. `host.docker.internal` lets the container reach
your API running on the host machine. Put this exact `docker run` command in
your README, as the brief requires.

## Step 7: Push to GitHub

Same pattern as Part 2 — put this in a clearly named branch or subfolder,
e.g. a `Part3` subfolder or a `part3` branch, in your existing `RaceDay-POE`
repo. Once pushed, `.github/workflows/dotnet-ci.yml` builds and tests all
three projects automatically — check the **Actions** tab for the green
checkmark and screenshot it for your README.

## Step 8: Update your README

Add a Part 3 section covering: how to run the MVC app, the roles and what
each can do, the exact `docker run` command from Step 6, the new CI
screenshot, and your Part 3 YouTube video link.

## If something doesn't build

- **"SDK not found"** — Visual Studio 2022 17.8+ with the ASP.NET and web
  development workload (includes .NET 8 SDK).
- **MVC pages can't reach the API ("connection refused")** — check
  `ApiBaseUrl` in `RaceDay.Mvc/appsettings.json` matches the API's actual
  port, and that the API project is actually running.
- **Image upload fails** — make sure either Azurite is running (Option A) or
  your real Azure connection string is in User Secrets (Option B).
- **Any red squiggly / build error** — copy the exact text from the Error
  List panel and send it to me.
