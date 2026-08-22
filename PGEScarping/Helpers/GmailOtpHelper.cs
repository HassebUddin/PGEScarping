using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using PGEScarping.Options;

namespace PGEScarping.Helpers;

public static class GmailOtpHelper
{
    public static async Task<string?> FetchLatestCodeAsync(EmailInboxOptions options, int withinMinutes, AppLogFile? logFile = null)
    {
        using var imapClient = new ImapClient();
        // Some networks block the OCSP/CRL revocation-check endpoints, which otherwise fails the TLS
        // handshake entirely even though the certificate itself is fine. Skip revocation checking so
        // the connection still succeeds on those networks.
        imapClient.CheckCertificateRevocation = false;
        await imapClient.ConnectAsync(options.ImapHost, options.ImapPort, SecureSocketOptions.SslOnConnect);
        await imapClient.AuthenticateAsync(options.Username, options.Password);
        await imapClient.Inbox.OpenAsync(FolderAccess.ReadOnly);

        var query = SearchQuery.DeliveredAfter(DateTime.Now.AddMinutes(-withinMinutes));
        var uids = await imapClient.Inbox.SearchAsync(query);

        logFile?.Append($"--- GmailOtpHelper: {uids.Count} email(s) found in the last {withinMinutes} minute(s) ---");

        string? foundCode = null;

        foreach (var uid in uids.OrderByDescending(u => u.Id))
        {
            var message = await imapClient.Inbox.GetMessageAsync(uid);
            var bodyText = message.TextBody ?? message.HtmlBody ?? "";

            logFile?.Append(
                $"From: {message.From} | To: {message.To} | Subject: {message.Subject} | Date: {message.Date}" +
                Environment.NewLine + "Body:" + Environment.NewLine + bodyText);

            if (foundCode is null)
            {
                var match = Regex.Match((message.Subject ?? "") + " " + bodyText, @"\b(\d{6})\b");
                if (match.Success)
                    foundCode = match.Groups[1].Value;
            }
        }

        await imapClient.DisconnectAsync(true);

        logFile?.Append(foundCode is null
            ? "--- GmailOtpHelper: no 6-digit code found in any of the above emails ---"
            : $"--- GmailOtpHelper: using code {foundCode} ---");

        return foundCode;
    }
}
