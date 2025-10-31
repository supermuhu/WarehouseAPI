// using Microsoft.Extensions.Options;
// using System.Net;
// using System.Net.Mail;
// using WarehouseAPI.Configuration;
// using WarehouseAPI.Helpers;

// namespace WarehouseAPI.Services
// {
//     public class EmailService
//     {
//         private readonly string email;
//         private readonly string password;
//         private readonly string host;
//         private readonly int port;
//         private readonly string hostingBE;
//         private readonly string hostingFE;
//         public EmailService(IOptions<EmailConfiguration> emailOptions, IOptions<HostConfiguration> hostingOptions)
//         {
//             email = emailOptions.Value.Email;
//             password = emailOptions.Value.Password;
//             host = emailOptions.Value.Host;
//             port = emailOptions.Value.Port;
//             hostingBE= hostingOptions.Value.Url;
//             hostingFE = hostingOptions.Value.FrontEndUrl;
//         }
//         public void SendEmailOTP(string receptor, string subject, string name, string otp)
//         {
//             var smtpClient = new SmtpClient(host, port)
//             {
//                 Credentials = new NetworkCredential(email, password),
//                 EnableSsl = true,
//             };
//             var body = $@"
//                 <!DOCTYPE html>
//                 <html lang='en'>
//                 <head>
//                     <meta charset='UTF-8'>
//                     <meta name='viewport' content='width=device-width, initial-scale=1.0'>
//                     <meta http-equiv='X-UA-Compatible' content='ie=edge'>
//                     <title>OTP</title>
//                 </head>
//                 <body>
//                     <div style=""font-family:Helvetica,Arial,sans-serif;min-width:1000px;overflow:auto;line-height:2"">
//                         <div style=""margin:50px auto;width:70%;padding:20px 0""><span class=""im"">
//                             <div style=""border-bottom:1px solid #eee"">
//                             <a href=""{hostingFE}"" style=""font-size:1.4em;color:#F05123;text-decoration:none;font-weight:600"">Baza Rental</a>
//                             </div>
//                             <p style=""font-size:1.1em"">Chào {name},</p>
//                             <p>Cảm ơn vì đã lựa chọn Baza. Mã OTP bên dưới sẽ hết hạn trong 5 phút.</p>
//                             </span><h2 style=""background:#F05123;margin:0 auto;width:max-content;padding:0 10px;color:#fff;border-radius:4px"">{otp}</h2>
//                             <p style=""font-size:0.9em"">Thân ái,<br>Baza</p>
//                             <hr style=""border:none;border-top:1px solid #eee""><div class=""yj6qo""></div><div class=""adL"">
//                         </div></div><div class=""adL"">
//                         </div>
//                     </div>
//                 </body>
//                 </html>
//             ";
//             var message = new MailMessage(email, receptor, subject, body)
//             {
//                 IsBodyHtml = true // Set this to true to enable HTML content
//             };
//             smtpClient.Send(message);
//         }
//     }
// }
