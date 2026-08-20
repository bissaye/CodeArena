namespace CodeArena.Infrastructure.Services;

internal static class EmailTemplates
{
    private const string PrimaryColor = "#6C63FF";
    private const string DangerColor = "#E74C3C";
    private const string TextColor = "#1A1A2E";
    private const string SubtextColor = "#6B7280";
    private const string BgColor = "#F5F5F5";
    private const string CardBg = "#FFFFFF";
    private const string FontFamily = "Arial, sans-serif";

    public static string BuildVerificationEmail(string username, string verifyUrl) => $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Vérifiez votre email</title></head>
<body style=""margin:0;padding:0;background-color:{BgColor};font-family:{FontFamily};"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:{BgColor};padding:40px 0;"">
    <tr><td align=""center"">
      <table width=""580"" cellpadding=""0"" cellspacing=""0"" style=""max-width:580px;width:100%;background-color:{CardBg};border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
        <!-- Header -->
        <tr><td style=""background-color:{PrimaryColor};padding:32px 40px;text-align:center;"">
          <h1 style=""margin:0;color:#FFFFFF;font-size:24px;font-weight:700;letter-spacing:1px;"">CodeArena</h1>
          <p style=""margin:4px 0 0;color:rgba(255,255,255,0.8);font-size:13px;"">Cameroun</p>
        </td></tr>
        <!-- Body -->
        <tr><td style=""padding:40px 40px 32px;"">
          <h2 style=""margin:0 0 16px;color:{TextColor};font-size:20px;font-weight:700;"">Vérifiez votre adresse email</h2>
          <p style=""margin:0 0 12px;color:{SubtextColor};font-size:15px;line-height:1.6;"">Bonjour <strong style=""color:{TextColor}"">{username}</strong>,</p>
          <p style=""margin:0 0 28px;color:{SubtextColor};font-size:15px;line-height:1.6;"">
            Merci de vous être inscrit sur CodeArena ! Cliquez sur le bouton ci-dessous pour confirmer votre adresse email et activer votre compte.
          </p>
          <!-- CTA Button -->
          <table cellpadding=""0"" cellspacing=""0"" style=""margin:0 auto 28px;"">
            <tr><td style=""background-color:{PrimaryColor};border-radius:8px;"">
              <a href=""{verifyUrl}"" style=""display:inline-block;padding:14px 36px;color:#FFFFFF;font-size:16px;font-weight:700;text-decoration:none;border-radius:8px;"">
                Vérifier mon email
              </a>
            </td></tr>
          </table>
          <p style=""margin:0 0 8px;color:{SubtextColor};font-size:13px;line-height:1.6;"">
            Ce lien expire dans <strong>1 heure</strong>. Si vous n'êtes pas à l'origine de cette inscription, ignorez cet email.
          </p>
          <p style=""margin:0;color:{SubtextColor};font-size:12px;word-break:break-all;"">
            Lien direct : <a href=""{verifyUrl}"" style=""color:{PrimaryColor}"">{verifyUrl}</a>
          </p>
        </td></tr>
        <!-- Footer -->
        <tr><td style=""background-color:#F9FAFB;padding:20px 40px;border-top:1px solid #E5E7EB;text-align:center;"">
          <p style=""margin:0;color:{SubtextColor};font-size:12px;"">© 2026 CodeArena Cameroun — Tous droits réservés</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

    public static string BuildPasswordResetEmail(string username, string resetUrl) => $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Réinitialisation mot de passe</title></head>
<body style=""margin:0;padding:0;background-color:{BgColor};font-family:{FontFamily};"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:{BgColor};padding:40px 0;"">
    <tr><td align=""center"">
      <table width=""580"" cellpadding=""0"" cellspacing=""0"" style=""max-width:580px;width:100%;background-color:{CardBg};border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
        <!-- Header -->
        <tr><td style=""background-color:{PrimaryColor};padding:32px 40px;text-align:center;"">
          <h1 style=""margin:0;color:#FFFFFF;font-size:24px;font-weight:700;letter-spacing:1px;"">CodeArena</h1>
          <p style=""margin:4px 0 0;color:rgba(255,255,255,0.8);font-size:13px;"">Cameroun</p>
        </td></tr>
        <!-- Body -->
        <tr><td style=""padding:40px 40px 32px;"">
          <h2 style=""margin:0 0 16px;color:{TextColor};font-size:20px;font-weight:700;"">Réinitialisation de votre mot de passe</h2>
          <p style=""margin:0 0 12px;color:{SubtextColor};font-size:15px;line-height:1.6;"">Bonjour <strong style=""color:{TextColor}"">{username}</strong>,</p>
          <p style=""margin:0 0 28px;color:{SubtextColor};font-size:15px;line-height:1.6;"">
            Nous avons reçu une demande de réinitialisation de votre mot de passe. Cliquez sur le bouton ci-dessous pour choisir un nouveau mot de passe.
          </p>
          <!-- CTA Button -->
          <table cellpadding=""0"" cellspacing=""0"" style=""margin:0 auto 28px;"">
            <tr><td style=""background-color:{DangerColor};border-radius:8px;"">
              <a href=""{resetUrl}"" style=""display:inline-block;padding:14px 36px;color:#FFFFFF;font-size:16px;font-weight:700;text-decoration:none;border-radius:8px;"">
                Réinitialiser mon mot de passe
              </a>
            </td></tr>
          </table>
          <p style=""margin:0 0 8px;color:{SubtextColor};font-size:13px;line-height:1.6;"">
            Ce lien expire dans <strong>1 heure</strong>. Si vous n'avez pas demandé cette réinitialisation, ignorez cet email — votre mot de passe reste inchangé.
          </p>
          <p style=""margin:0;color:{SubtextColor};font-size:12px;word-break:break-all;"">
            Lien direct : <a href=""{resetUrl}"" style=""color:{DangerColor}"">{resetUrl}</a>
          </p>
        </td></tr>
        <!-- Footer -->
        <tr><td style=""background-color:#F9FAFB;padding:20px 40px;border-top:1px solid #E5E7EB;text-align:center;"">
          <p style=""margin:0;color:{SubtextColor};font-size:12px;"">© 2026 CodeArena Cameroun — Tous droits réservés</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

    public static string BuildWelcomeEmail(string username, string appUrl) => $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><title>Bienvenue sur CodeArena</title></head>
<body style=""margin:0;padding:0;background-color:{BgColor};font-family:{FontFamily};"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:{BgColor};padding:40px 0;"">
    <tr><td align=""center"">
      <table width=""580"" cellpadding=""0"" cellspacing=""0"" style=""max-width:580px;width:100%;background-color:{CardBg};border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
        <!-- Header -->
        <tr><td style=""background-color:{PrimaryColor};padding:32px 40px;text-align:center;"">
          <h1 style=""margin:0;color:#FFFFFF;font-size:24px;font-weight:700;letter-spacing:1px;"">CodeArena</h1>
          <p style=""margin:4px 0 0;color:rgba(255,255,255,0.8);font-size:13px;"">Cameroun</p>
        </td></tr>
        <!-- Body -->
        <tr><td style=""padding:40px 40px 32px;"">
          <h2 style=""margin:0 0 16px;color:{TextColor};font-size:20px;font-weight:700;"">Bienvenue, {username} !</h2>
          <p style=""margin:0 0 20px;color:{SubtextColor};font-size:15px;line-height:1.6;"">
            Votre compte CodeArena est prêt. Rejoignez les compétitions de programmation et grimpez dans le classement camerounais !
          </p>
          <!-- Features -->
          <table cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-bottom:28px;"">
            <tr><td style=""padding:12px 16px;background-color:#F3F0FF;border-radius:8px;margin-bottom:8px;"">
              <p style=""margin:0;color:{TextColor};font-size:14px;""><strong>🏆 Compétitions</strong> — Participez aux challenges en cours</p>
            </td></tr>
            <tr><td style=""height:8px;""></td></tr>
            <tr><td style=""padding:12px 16px;background-color:#F3F0FF;border-radius:8px;margin-bottom:8px;"">
              <p style=""margin:0;color:{TextColor};font-size:14px;""><strong>📊 Classement</strong> — Suivez votre progression nationale</p>
            </td></tr>
            <tr><td style=""height:8px;""></td></tr>
            <tr><td style=""padding:12px 16px;background-color:#F3F0FF;border-radius:8px;"">
              <p style=""margin:0;color:{TextColor};font-size:14px;""><strong>💡 Exercices</strong> — Résolvez des problèmes algorithmiques</p>
            </td></tr>
          </table>
          <!-- CTA Button -->
          <table cellpadding=""0"" cellspacing=""0"" style=""margin:0 auto;"">
            <tr><td style=""background-color:{PrimaryColor};border-radius:8px;"">
              <a href=""{appUrl}"" style=""display:inline-block;padding:14px 36px;color:#FFFFFF;font-size:16px;font-weight:700;text-decoration:none;border-radius:8px;"">
                Aller sur CodeArena
              </a>
            </td></tr>
          </table>
        </td></tr>
        <!-- Footer -->
        <tr><td style=""background-color:#F9FAFB;padding:20px 40px;border-top:1px solid #E5E7EB;text-align:center;"">
          <p style=""margin:0;color:{SubtextColor};font-size:12px;"">© 2026 CodeArena Cameroun — Tous droits réservés</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
}
