# Email delivery

The application sends enrollment, approval, and rejection messages through the
Brevo HTTPS API (`POST https://api.brevo.com/v3/smtp/email`). It does not use
Gmail SMTP. Brevo uses HTTPS on port 443, and the app's HTTP client timeout is
15 seconds.

## Railway variables

Set these variables on the Ascendia service:

| Variable | Required | Description |
| --- | --- | --- |
| `Brevo__ApiKey` | Yes | Brevo API key. Never commit or share it. |
| `Brevo__SenderEmail` | Yes | Verified sender email address in Brevo. |
| `Brevo__SenderName` | No | Sender display name; defaults to `Ascendia Registrar`. |

`EmailSettings__SenderEmail` is accepted as a temporary fallback for the sender
address, but `EmailSettings__SenderPassword` is not used by Brevo. The old Gmail
app password cannot authenticate with the Brevo API. Prefer the `Brevo__*`
variables above and remove the unused password variable from Railway.

## Verify the sender and create an API key

1. Sign in to Brevo and open **Settings → Senders, Domains & Dedicated IPs**.
2. Add the email address to use as `Brevo__SenderEmail` and complete Brevo's
   sender verification. A custom domain is not required for a verified sender
   address, though domain authentication can improve deliverability.
3. Open **SMTP & API → API Keys**, create an API key, and save it directly as
   the Railway variable `Brevo__ApiKey`.
4. Set `Brevo__SenderName` if the default display name is not desired, then
   redeploy/restart the Railway service so the variables are loaded.

## Verify in production

1. Sign in as a registrar and open `/Admin/TestEmail?to=your-address@example.com`.
2. Submit the test form. A successful API response confirms Brevo accepted the
   message; also check the destination inbox and spam folder.
3. Approve or reject a test application and review the Railway application
   logs.

Expected log entries include `Sending ... email via Brevo to ...`,
`Brevo accepted ... email to ...`, and the handler's approval/rejection
notification success message. Configuration omissions log
`Brevo email is not configured; skipping ...`; API or network failures log a
full exception with `Brevo request failed` or `Brevo rejected`. The API key and
sender password are never logged. An email failure does not roll back an
approval or prevent a rejection from completing.
