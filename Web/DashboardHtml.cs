namespace ChatUnifier.Web
{
    public static class DashboardHtml
    {
        public static string Build(int port, bool youtubeConnected, bool youtubeConfigured)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>ChatUnifier</title>
<style>
  * {{ box-sizing: border-box; margin: 0; padding: 0; }}
  body {{ font-family: 'Segoe UI', sans-serif; background: #0e0e10; color: #efeff1; min-height: 100vh; display: flex; align-items: center; justify-content: center; }}
  .container {{ width: 480px; padding: 32px; }}
  h1 {{ font-size: 1.6rem; font-weight: 700; margin-bottom: 4px; }}
  .subtitle {{ color: #adadb8; font-size: 0.9rem; margin-bottom: 32px; }}
  .card {{ background: #18181b; border: 1px solid #2a2a2e; border-radius: 10px; padding: 20px; margin-bottom: 16px; }}
  .card-header {{ display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }}
  .platform {{ display: flex; align-items: center; gap: 10px; font-weight: 600; font-size: 1rem; }}
  .dot {{ width: 10px; height: 10px; border-radius: 50%; }}
  .dot-green {{ background: #00c853; box-shadow: 0 0 6px #00c85380; }}
  .dot-red {{ background: #ff4444; }}
  .status-text {{ font-size: 0.82rem; color: #adadb8; }}
  .fields {{ display: flex; flex-direction: column; gap: 10px; margin-bottom: 14px; }}
  input {{ background: #0e0e10; border: 1px solid #3a3a3e; border-radius: 6px; color: #efeff1; padding: 9px 12px; font-size: 0.88rem; width: 100%; outline: none; }}
  input:focus {{ border-color: #6441a5; }}
  .btn {{ display: inline-block; padding: 9px 20px; border-radius: 6px; font-size: 0.88rem; font-weight: 600; border: none; cursor: pointer; width: 100%; text-align: center; transition: opacity .15s; }}
  .btn:hover {{ opacity: 0.85; }}
  .btn-twitch {{ background: #6441a5; color: #fff; }}
  .btn-youtube {{ background: #ff0000; color: #fff; }}
  .btn-disconnect {{ background: #2a2a2e; color: #adadb8; margin-top: 8px; }}
  .connected-info {{ font-size: 0.83rem; color: #adadb8; margin-bottom: 10px; }}
  .save-msg {{ font-size: 0.8rem; color: #00c853; margin-top: 6px; display: none; }}
  .dropzone {{ border: 1px dashed #3a3a3e; border-radius: 8px; padding: 12px; color: #adadb8; font-size: 0.85rem; background: #0e0e10; }}
  .dropzone.dragover {{ border-color: #ff0000; color: #efeff1; }}
  .small {{ font-size: 0.78rem; color: #adadb8; margin-top: 6px; line-height: 1.3; }}
</style>
</head>
<body>
<div class=""container"">
  <h1>ChatUnifier</h1>
  <p class=""subtitle"">YouTube Live Chat → ChatPlex</p>

  <div class=""card"" id=""youtube-card"">
    <div class=""card-header"">
      <div class=""platform"">
        <svg width=""18"" height=""18"" viewBox=""0 0 24 24"" fill=""#ff0000""><path d=""M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z""/></svg>
        YouTube
      </div>
      <div style=""display:flex;align-items:center;gap:8px"">
        <div class=""dot {(youtubeConnected ? "dot-green" : "dot-red")}""></div>
        <span class=""status-text"">{(youtubeConnected ? "Connected" : "Disconnected")}</span>
      </div>
    </div>

    {(youtubeConnected
        ? $@"<p class=""connected-info"">Polling your active livestream chat.</p>
             <form onsubmit=""disconnectYouTube(event)"">
               <button class=""btn btn-disconnect"" type=""submit"">Disconnect YouTube</button>
             </form>"
        : $@"<div class=""fields"">
               <div id=""google-drop"" class=""dropzone"">
                 Drag & drop your Google OAuth client JSON here
                 <div class=""small"">(Google Cloud → APIs & Services → Credentials → OAuth client → Download JSON)</div>
               </div>
               <input id=""google-client-id"" type=""text"" placeholder=""Google Client ID"" value=""{(youtubeConfigured ? "••••••••••••••••" : "")}"">
               <input id=""google-client-secret"" type=""password"" placeholder=""Google Client Secret"" value=""{(youtubeConfigured ? "••••••••••••••••" : "")}"">
             </div>
             <form onsubmit=""connectYouTube(event)"">
               <button class=""btn btn-youtube"" type=""submit"">Connect YouTube</button>
             </form>
             <p class=""save-msg"" id=""youtube-save-msg"">Credentials saved!</p>"
    )}
  </div>
</div>

<script>
const PORT = {port};

function setupDropzone() {{
  const dz = document.getElementById('google-drop');
  if (!dz) return;

  const prevent = (e) => {{ e.preventDefault(); e.stopPropagation(); }};
  ['dragenter','dragover','dragleave','drop'].forEach(evt => dz.addEventListener(evt, prevent));
  dz.addEventListener('dragover', () => dz.classList.add('dragover'));
  dz.addEventListener('dragleave', () => dz.classList.remove('dragover'));

  dz.addEventListener('drop', async (e) => {{
    dz.classList.remove('dragover');
    const f = e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0];
    if (!f) return;
    try {{
      const parsed = JSON.parse(await f.text());
      const r = await fetch('/import/google-client', {{
        method: 'POST',
        headers: {{'Content-Type':'application/json'}},
        body: JSON.stringify(parsed)
      }});
      const out = await r.json();
      if (!out.ok) {{ alert(out.error || 'Import failed'); return; }}
      const msg = document.getElementById('youtube-save-msg');
      if (msg) {{ msg.style.display = 'block'; msg.textContent = 'Imported from JSON!'; }}
      setTimeout(() => location.reload(), 600);
    }} catch (err) {{
      alert('That file did not look like a Google OAuth JSON.');
    }}
  }});
}}

async function connectYouTube(e) {{
  e.preventDefault();
  const id = document.getElementById('google-client-id').value.trim();
  const secret = document.getElementById('google-client-secret').value.trim();
  if (!id || !secret || id.startsWith('•')) {{ window.location.href = '/login/youtube'; return; }}
  await fetch('/credentials/youtube', {{
    method: 'POST',
    headers: {{'Content-Type':'application/json'}},
    body: JSON.stringify({{client_id: id, client_secret: secret}})
  }});
  const msg = document.getElementById('youtube-save-msg');
  if (msg) {{ msg.style.display = 'block'; }}
  setTimeout(() => window.location.href = '/login/youtube', 800);
}}

async function disconnectYouTube(e) {{
  e.preventDefault();
  await fetch('/disconnect/youtube', {{ method: 'POST' }});
  location.reload();
}}

setupDropzone();
</script>
</body>
</html>";
        }
    }
}
