const PORT = 27839;
const statusEl = document.getElementById('status');
const sendBtn = document.getElementById('sendBtn');
const copyBtn = document.getElementById('copyBtn');
const tokenBox = document.getElementById('tokenBox');

let extractedToken = null;

function setStatus(msg, cls) {
  statusEl.textContent = msg;
  statusEl.className = cls;
}

function showToken(token) {
  extractedToken = token;
  tokenBox.value = token;
  tokenBox.style.display = 'block';
  copyBtn.style.display = 'block';
}

sendBtn.addEventListener('click', async () => {
  sendBtn.disabled = true;
  setStatus('Extracting token...', 'working');

  try {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

    if (!tab?.url?.includes('web.plaud.ai')) {
      setStatus('Navigate to web.plaud.ai first.', 'error');
      sendBtn.disabled = false;
      return;
    }

    const results = await chrome.scripting.executeScript({
      target: { tabId: tab.id },
      func: () => localStorage.getItem('tokenstr')
    });

    const token = results?.[0]?.result;
    if (!token) {
      setStatus('No token found. Are you logged in?', 'error');
      sendBtn.disabled = false;
      return;
    }

    // Always show the token as fallback
    showToken(token);

    setStatus('Sending to Transcriptonator...', 'working');

    try {
      const resp = await fetch(`http://localhost:${PORT}/plaud-token`, {
        method: 'POST',
        headers: { 'Content-Type': 'text/plain' },
        body: token
      });

      if (resp.ok) {
        setStatus('Token sent to app!', 'success');
        return;
      }
    } catch (e) {
      // App not reachable - fall through to fallback
    }

    setStatus('App not reachable. Copy the token below and paste it in the app.', 'error');
    sendBtn.disabled = false;
  } catch (err) {
    setStatus(`Error: ${err.message}`, 'error');
    sendBtn.disabled = false;
  }
});

copyBtn.addEventListener('click', async () => {
  if (extractedToken) {
    try {
      await navigator.clipboard.writeText(extractedToken);
      setStatus('Token copied to clipboard! Paste it in the app.', 'success');
    } catch {
      tokenBox.select();
      document.execCommand('copy');
      setStatus('Token copied! Paste it in the app.', 'success');
    }
  }
});
