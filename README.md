# ChatUnifier
**ChatUnifier** essentially bypasses normal ChatPlex servers, giving you the option to get YouTube chat without having to use the servers (Which are Patreon only!)
---
## Overview
ChatUnifier allows you to add your own YouTube API key, bypassing normal ChatPlex servers.
You use your own API key and secret from your Google Cloud Console, give the JSON secret to ChatUnified through the web interface, and done!
---
## Known issues
1. You have to click "Disconnect Youtube" and then reconnect in the WebUI in order to get it to load
   - Make this automatic, or a button in UI? (So we don't eat up our quota)
---
## How to setup!
# Getting Your YouTube OAuth Credentials for ChatUnifier
## Prerequisites
- A Google account
- An active YouTube channel
---
## Step 1: Go to Google Cloud Console
1. Open your browser and go to [https://console.cloud.google.com](https://console.cloud.google.com)
2. Sign in with your Google account
---
## Step 2: Create a New Project
1. Click the project dropdown at the top of the page
2. Click **New Project**
3. Give it any name (e.g. `ChatUnifier`)
4. Click **Create**
5. Wait for it to create, then make sure it's selected in the dropdown
---
## Step 3: Enable the YouTube Data API
1. In the left sidebar, go to **APIs & Services** → **Library**
2. Search for `YouTube Data API v3`
3. Click on it, then click **Enable**
---
## Step 4: Configure the OAuth Consent Screen
1. Go to **APIs & Services** → **OAuth consent screen**
2. Select **External** and click **Create**
3. Fill in the required fields:
   - **App name**: anything you like (e.g. `ChatUnifier`)
   - **User support email**: your email
   - **Developer contact email**: your email
4. Click **Save and Continue** through the remaining steps until done
5. In the left sidebar, go to **OAuth consent screen** → **Audience** → **Test users**
6. Click **+ Add Users**
7. Enter the Google account email you stream with
8. Click **Save**
---
## Step 5: Create OAuth Credentials
1. Go to **APIs & Services** → **Credentials**
2. Click **+ Create Credentials** → **OAuth client ID**
3. Set **Application type** to **Web Application**
4. Give it any name (e.g. `ChatUnifier`)
5. Under **Authorised redirect URIs**, click **+ Add URI**
6. Enter: `http://localhost:42069/callback/youtube`
7. Click **Add**
8. Click **Create**
---
## Step 6: Download the JSON
1. You will see a popup — click **Download JSON**
2. Save the file somewhere you can find it
3. If you are unable to download it from this prompt, Go to **Clients**, and under **Client Secrets** there should be **Client Secret**. Click the download button next to it.
---
## Step 7: Connect in ChatUnifier
1. Launch Beat Saber — your browser will open the ChatUnifier dashboard automatically
2. Drag and drop the downloaded JSON file onto the **Settings WebUI** page that opens.
3. Click **Connect YouTube**
4. Sign in with your Google account and grant the requested permissions
5. You should see the status change to **Connected**
---
## Notes
- You only need to do this once — your token is saved locally
- If you ever get disconnected, just open `http://localhost:42069` in your browser and reconnect
- If it ever stops working, click "Disconnect Youtube" and then reconnect it, it should work.
