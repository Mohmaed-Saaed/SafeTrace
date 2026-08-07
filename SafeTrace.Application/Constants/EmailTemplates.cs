using System.Net;
using SafeTrace.Application.Helpers;

namespace SafeTrace.Application.Constants
{
    public static class EmailTemplates
    {
        public const string UrgentCaseDetailsRoute = "/urgent/";
        public const string LongTermCaseDetailsRoute = "/long-term/";
        public const string UnknownCaseDetailsRoute = "/unknown/";
        public const string PrimaryColor = "#091426";
        public const string SecondaryColor = "#75777D";
        public const string ButtonColor = "#0058BE";
        public const string CompanyName = "منصة لقاء";
        public const string Footer = "فريق عمل منصة لقاء";
        public const string WebsiteUrl = SystemConstants.BaseUrl;
        public const string LogoUrl = SystemConstants.BaseUrl + "/Images/logo.jpg";

        public static string GetCaseDetailsUrl(CaseType caseType, long caseId) =>
            $"{SystemConstants.BaseUrl}{GetCaseDetailsRoute(caseType)}{caseId}";

        public static string GetCaseDetailsRoute(CaseType caseType) => caseType switch
        {
            CaseType.Urgent => UrgentCaseDetailsRoute,
            CaseType.LongTerm => LongTermCaseDetailsRoute,
            CaseType.Unknown => UnknownCaseDetailsRoute,
            _ => throw new ArgumentOutOfRangeException(nameof(caseType), caseType, null)
        };

        private static string WrapInBaseLayout(string contentHtml)
        {
            return $@"<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <style>
        body, table, td, a {{ -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; }}
        table, td {{ mso-table-lspace: 0pt; mso-table-rspace: 0pt; }}
        img {{ -ms-interpolation-mode: bicubic; border: 0; outline: none; text-decoration: none; }}
        
        @media only screen and (max-width: 600px) {{
            .email-outer-padding {{ padding: 16px 8px !important; }}
            .email-card {{ border-radius: 14px !important; }}
            .email-header {{ padding: 28px 16px !important; }}
            .email-body {{ padding: 24px 16px !important; }}
            .email-title {{ font-size: 22px !important; }}
            .email-subtitle {{ font-size: 11px !important; }}
            .email-h2 {{ font-size: 19px !important; }}
            .email-text {{ font-size: 14px !important; line-height: 1.7 !important; }}
            .email-btn {{ display: block !important; width: 100% !important; box-sizing: border-box !important; padding: 14px 16px !important; font-size: 15px !important; text-align: center !important; }}
            .email-otp {{ font-size: 24px !important; letter-spacing: 5px !important; }}
            .email-card-box {{ padding: 16px 14px !important; border-radius: 12px !important; }}
            .info-table td {{ padding: 10px 12px !important; font-size: 13px !important; }}
            .info-table-label {{ width: 45% !important; }}
        }}
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; -webkit-font-smoothing: antialiased;'>
    <div dir='rtl' class='email-outer-padding' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
        <div class='email-card' style='max-width: 600px; width: 100%; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden; box-sizing: border-box;'>
            <div class='email-header' style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                    <a href='{WebsiteUrl}' target='_blank' style='text-decoration: none; display: block;'>
                        <img src='{LogoUrl}' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto; max-width: 100%; height: auto;' />
                    </a>
                </div>
                <h1 class='email-title' style='margin: 0 0 6px 0;'>
                    <a href='{WebsiteUrl}' target='_blank' style='color: #FFFFFF; text-decoration: none; font-size: 28px; font-weight: 800; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>{CompanyName}</a>
                </h1>
                <p class='email-subtitle' style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
            </div>
            
            <div class='email-body' style='padding: 36px 32px; box-sizing: border-box; word-break: break-word; overflow-wrap: break-word;'>
                {contentHtml}
                
                <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                <div style='text-align: right;'>
                    <p class='email-text' style='color: #0B1C30; font-size: 15px; margin: 0 0 8px 0; font-weight: 700;'>
                        مع خالص التحية،<br/>
                        <a href='{WebsiteUrl}' target='_blank' style='color: #0058BE; text-decoration: none; font-weight: 700;'>{Footer}</a>
                    </p>
                    <p style='margin: 6px 0 0 0; font-size: 13px; color: #75777D;'>
                        🌐 زيارة المنصة: <a href='{WebsiteUrl}' target='_blank' style='color: #0058BE; font-weight: 600; text-decoration: underline;' dir='ltr'>{WebsiteUrl}</a>
                    </p>
                </div>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        public static string BuildArabicOtpEmailTemplate(string fullName, string otpCode, string contextTitle, string contextualDescription)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 28px 0;'>{contextualDescription}</p>
                
                <div class='email-card-box' style='background-color: #F8F9FF; border: 2px dashed #0058BE; padding: 24px 20px; border-radius: 18px; text-align: center; margin: 28px 0; box-sizing: border-box; max-width: 100%; word-break: break-all;'>
                    <span style='font-size: 13px; color: #0058BE; display: block; margin-bottom: 10px; font-weight: 700;'>{contextTitle}</span>
                    <span class='email-otp' style='font-size: 36px; font-weight: 800; color: #0058BE; letter-spacing: 10px; font-family: monospace; display: inline-block;'>{otpCode}</span>
                </div>
                
                <div class='email-card-box' style='background-color: #FFFBEB; border: 1px solid #F59E0B; padding: 16px 20px; border-radius: 14px; margin-bottom: 24px; box-sizing: border-box; max-width: 100%;'>
                    <p style='color: #B45309; font-size: 13px; font-weight: 600; margin: 0; line-height: 1.7;'>⚠️ <strong>تنبيه أمني:</strong> تنتهي صلاحية هذا الرمز بعد 10 دقائق تلقائياً. يرجى عدم مشاركة هذا الرمز مع أي شخص كائن من كان لحماية سرية حسابك.</p>
                </div>
                
                <p class='email-text' style='color: #75777D; font-size: 13px; line-height: 1.8; margin: 0;'>إذا لم تقم بإنشاء هذا الحساب أو تقديم هذا الطلب، يمكنك تجاهل هذا البريد الإلكتروني بأمان دون اتخاذ أي إجراء إضافي.</p>");
        }

        public static string BuildArabicNewMessageEmailTemplate(
            string receiverName,
            string senderName,
            string messagePreview,
            string chatLink)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً {receiverName}</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>لديك رسالة جديدة من <strong style='color: #0058BE;'>{senderName}</strong></p>

                <div class='email-card-box' style='background-color: #F8F9FF; border: 1px solid #D3E4FE; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #0B1C30; font-size: 15px; line-height: 1.8; border-right: 4px solid #0058BE; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    <div style='font-weight: 700; color: #0058BE; margin-bottom: 8px; font-size: 13px;'>💬 معاينة الرسالة:</div>
                    {messagePreview}
                </div>

                <div style='text-align: center; margin: 32px 0;'>
                    <a href='{chatLink}' class='email-btn' style='background: linear-gradient(135deg, #0058BE 0%, #091426 100%); color: #ffffff; padding: 14px 32px; border-radius: 12px; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block; box-shadow: 0 4px 14px rgba(0, 88, 190, 0.3); max-width: 100%; box-sizing: border-box; word-break: break-word;'>
                        عرض الرسالة
                    </a>
                </div>

                <p class='email-text' style='color: #75777D; font-size: 13px; line-height: 1.8; margin: 0;'>إذا لم تكن تتوقع هذه الرسالة، يمكنك تجاهل هذا البريد بأمان.</p>");
        }

        public static string BuildAdminRegisteredTemplate(string fullName, string email, string role)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>تم إنشاء حساب جديد لك في منصة لقاء من قِبل الإدارة. يمكنك الآن تفعيل حسابك عن طريق تعيين كلمة مرور جديدة من خلال الرابط أدناه:</p>
                
                <div class='email-card-box' style='background-color: #F8F9FF; border: 1px solid #C5C6CD; border-radius: 16px; overflow: hidden; margin: 24px 0; box-sizing: border-box; max-width: 100%;'>
                    <table class='info-table' role='presentation' cellSpacing='0' cellPadding='0' border='0' width='100%' style='border-collapse: collapse; font-size: 15px; width: 100%;'>
                        <tr style='border-bottom: 1px solid #E2E8F0;'>
                            <td class='info-table-label' style='padding: 14px 20px; color: #75777D; font-weight: 600; width: 40%; word-break: break-word;'>البريد الإلكتروني:</td>
                            <td style='padding: 14px 20px; color: #0058BE; font-weight: 700; word-break: break-all;' dir='ltr'>{email}</td>
                        </tr>
                        <tr style='background-color: #FFFFFF;'>
                            <td class='info-table-label' style='padding: 14px 20px; color: #75777D; font-weight: 600; word-break: break-word;'>الدور الممنوح:</td>
                            <td style='padding: 14px 20px; color: #0B1C30; font-weight: 700; word-break: break-word;'>{TranslateRoleToArabicHelper.TranslateRoleToArabic(role)}</td>
                        </tr>
                    </table>
                </div>
                
                <div style='text-align: center; margin: 32px 0;'>
                    <a href='{SystemConstants.BaseUrl}/auth/forgot-password' class='email-btn' style='background: linear-gradient(135deg, #0058BE 0%, #091426 100%); color: #ffffff; padding: 14px 32px; border-radius: 12px; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block; box-shadow: 0 4px 14px rgba(0, 88, 190, 0.3); max-width: 100%; box-sizing: border-box; word-break: break-word;'>
                        تعيين كلمة المرور
                    </a>
                </div>

                <div class='email-card-box' style='background-color: #FFFBEB; border: 1px solid #F59E0B; padding: 16px 20px; border-radius: 14px; margin-bottom: 24px; box-sizing: border-box; max-width: 100%;'>
                    <p style='color: #B45309; font-size: 13px; font-weight: 600; margin: 0; line-height: 1.7;'>📌 <strong>ملاحظة هامة:</strong> يجب تعيين كلمة المرور لتتمكن من تسجيل الدخول والمشاركة معنا.</p>
                </div>");
        }

        public static string BuildRoleChangedTemplate(string fullName, string newRole)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>نعلمك بأنه قد تم تحديث الصلاحية الإدارية (الدور) الخاص بحسابك في منصة لقاء بواسطة الإدارة.</p>
                
                <div class='email-card-box' style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #006B60; font-size: 16px; font-weight: 700; text-align: center; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    🛡️ دورك الجديد هو: <span style='color: #091426;'>{TranslateRoleToArabicHelper.TranslateRoleToArabic(newRole)}</span>
                </div>");
        }

        public static string BuildPermissionsChangedTemplate(string fullName)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>نعلمك بأنه قد تم تحديث <strong style='color: #0058BE;'>الصلاحيات الفردية (الاستثنائية)</strong> الممنوحة لحسابك في منصة لقاء بواسطة الإدارة.</p>
                
                <div class='email-card-box' style='background-color: #F8F9FF; border: 1px solid #D3E4FE; padding: 20px 24px; border-radius: 16px; margin: 24px 0; box-sizing: border-box; max-width: 100%;'>
                    <p style='color: #75777D; font-size: 14px; margin: 0; text-align: center; line-height: 1.7;'>🔐 تم تخصيص هذه الصلاحيات لتتناسب مع مهامك الحالية في النظام.</p>
                </div>");
        }

        public static string BuildVerificationApprovedTemplate(string fullName)
        {
            return WrapInBaseLayout($@"
                <div style='text-align: center; margin-bottom: 20px;'>
                    <span style='font-size: 48px; display: block; margin-bottom: 8px;'>🎉</span>
                    <h2 class='email-h2' style='color: #091426; font-size: 24px; font-weight: 800; margin: 0;'>تهانينا يا {fullName}!</h2>
                </div>
                
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>يسعدنا إخبارك بأنه تمت مراجعة صورة هويتك وقبولها بنجاح.</p>
                
                <div class='email-card-box' style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #006B60; font-size: 15px; line-height: 1.8; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    ✓ حسابك الآن موثق بالكامل ويمتلك صلاحيات <strong style='color: #091426;'>مستخدم موثق</strong>، مما يتيح لك الاستفادة من ميزات إضافية مثل إضافة الحالات طويلة المدى وحالات مجهولي الهوية.
                </div>");
        }

        public static string BuildVerificationRejectedTemplate(string fullName, string rejectionReason)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                
                <div class='email-card-box' style='background-color: #FFEDEC; border: 1px solid #BA1A1A; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #BA1A1A; font-size: 15px; font-weight: 700; text-align: center; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    ❌ عذراً، لم نتمكن من الموافقة على طلب توثيق حسابك في الوقت الحالي.
                </div>
                
                <div class='email-card-box' style='background-color: #F8F9FF; border: 1px solid #C5C6CD; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #0B1C30; font-size: 15px; line-height: 1.8; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    <div style='font-weight: 700; color: #091426; margin-bottom: 8px;'>سبب الرفض:</div>
                    {rejectionReason}
                </div>");
        }

        public static string BuildBlockStatusChangedTemplate(string fullName, bool isBlocked, string? blockReason = null)
        {
            string status = isBlocked ? "حظر" : "إلغاء الحظر عن";
            string message = isBlocked
                ? "تم تعليق حسابك في منصة لقاء. يرجى التواصل معنا إذا كنت تعتقد أن هذا حدث عن طريق الخطأ."
                : "تم تفعيل حسابك مرة أخرى في منصة لقاء. يمكنك الآن تسجيل الدخول والمشاركة.";
            
            string bgClass = isBlocked ? "background-color: #FFEDEC; border: 1px solid #BA1A1A; color: #BA1A1A;" : "background-color: #E6F6F4; border: 1px solid #00A292; color: #006B60;";

            var reasonBlock = isBlocked && !string.IsNullOrWhiteSpace(blockReason)
                ? $@"<div class='email-card-box' style='background-color: #F8F9FF; border: 1px solid #C5C6CD; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #0B1C30; font-size: 15px; line-height: 1.8; text-align: right; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                        <div style='font-weight: 700; color: #091426; margin-bottom: 8px;'>سبب الحظر:</div>
                        {blockReason}
                    </div>"
                : "";

            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>تم تحديث حالة حسابك في منصة لقاء بواسطة الإدارة.</p>
                
                <div class='email-card-box' style='{bgClass} padding: 20px 24px; border-radius: 16px; margin: 24px 0; font-weight: 700; font-size: 16px; text-align: center; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    الإجراء: {status} الحساب
                </div>

                {reasonBlock}
                
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>{message}</p>");
        }

        public static string BuildLoginAlertTemplate(string fullName, string ipAddress, string browser, string os)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #BA1A1A; font-size: 22px; font-weight: 800; margin: 0 0 16px 0;'>🔔 تنبيه أمني: تسجيل دخول جديد</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>مرحباً {fullName}، تم تسجيل دخول جديد إلى حسابك في منصة لقاء بالتفاصيل التالية:</p>
                
                <div class='email-card-box' style='background-color: #F8F9FF; border: 1px solid #C5C6CD; border-radius: 16px; overflow: hidden; margin: 24px 0; box-sizing: border-box; max-width: 100%;'>
                    <table class='info-table' role='presentation' cellSpacing='0' cellPadding='0' border='0' width='100%' style='border-collapse: collapse; font-size: 15px; width: 100%;'>
                        <tr style='border-bottom: 1px solid #E2E8F0;'>
                            <td class='info-table-label' style='padding: 14px 20px; color: #75777D; font-weight: 600; width: 35%; word-break: break-word;'>IP:</td>
                            <td style='padding: 14px 20px; color: #0058BE; font-weight: 700; word-break: break-all;' dir='ltr'>{ipAddress}</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #E2E8F0; background-color: #FFFFFF;'>
                            <td class='info-table-label' style='padding: 14px 20px; color: #75777D; font-weight: 600;'>Browser:</td>
                            <td style='padding: 14px 20px; color: #0B1C30; font-weight: 700; word-break: break-word;' dir='ltr'>{browser}</td>
                        </tr>
                        <tr style='background-color: #FFFFFF;'>
                            <td class='info-table-label' style='padding: 14px 20px; color: #75777D; font-weight: 600;'>OS:</td>
                            <td style='padding: 14px 20px; color: #0B1C30; font-weight: 700; word-break: break-word;' dir='ltr'>{os}</td>
                        </tr>
                    </table>
                </div>
                
                <div class='email-card-box' style='background-color: #FFEDEC; border: 1px solid #BA1A1A; padding: 16px 20px; border-radius: 14px; margin-bottom: 24px; text-align: center; box-sizing: border-box; max-width: 100%;'>
                    <p style='color: #BA1A1A; font-size: 14px; font-weight: 700; margin: 0;'>🚨 إذا لم تكن أنت من قام بتسجيل الدخول، يرجى تغيير كلمة المرور فوراً لحماية حسابك.</p>
                </div>");
        }

        public static string BuildPasswordResetSuccessTemplate(string fullName)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                
                <div class='email-card-box' style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 24px 20px; border-radius: 16px; margin: 24px 0; color: #006B60; text-align: center; box-sizing: border-box; max-width: 100%;'>
                    <span style='font-size: 32px; display: block; margin-bottom: 10px;'>✅</span>
                    <strong style='font-size: 17px;'>تم إعادة تعيين / تغيير كلمة المرور الخاصة بحسابك بنجاح.</strong>
                </div>
                
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 16px 0;'>تم تسجيل الخروج من كافة الأجهزة الأخرى لضمان أمان حسابك.</p>
                
                <div class='email-card-box' style='background-color: #FFEDEC; border: 1px solid #BA1A1A; padding: 16px 20px; border-radius: 14px; margin-bottom: 24px; box-sizing: border-box; max-width: 100%;'>
                    <p style='color: #BA1A1A; font-size: 14px; font-weight: 600; margin: 0; line-height: 1.7;'>⚠️ إذا لم تقم بهذا الإجراء، يرجى <strong style='text-decoration: underline;'>تقديم شكوى من خلال المنصة</strong> فوراً ليتمكن فريقنا من التدخل.</p>
                </div>");
        }

        public static string BuildEmailConfirmedSuccessTemplate(string fullName)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                
                <div class='email-card-box' style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 24px 20px; border-radius: 16px; margin: 24px 0; color: #006B60; text-align: center; box-sizing: border-box; max-width: 100%;'>
                    <span style='font-size: 32px; display: block; margin-bottom: 10px;'>✉️ ✅</span>
                    <strong style='font-size: 17px;'>تم تأكيد عنوان بريدك الإلكتروني بنجاح!</strong>
                </div>
                
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>شكراً لك على تأكيد بريدك الإلكتروني. يمكنك الآن الاستفادة من كافة خدمات وميزات منصة لقاء بكامل طاقتها.</p>");
        }

        public static string BuildGoogleRegistrationWelcomeTemplate(string fullName)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً بك، {fullName}</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>يسعدنا انضمامك إلى منصة لقاء! لقد قمت بالتسجيل بنجاح باستخدام حساب جوجل الخاص بك.</p>
                
                <div class='email-card-box' style='background-color: #F8F9FF; border: 1px solid #D3E4FE; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #0B1C30; font-size: 15px; line-height: 1.8; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    يمكنك دائماً تسجيل الدخول بسهولة وبضغطة زر باستخدام جوجل. 
                    ولكن إذا كنت تفضل في أي وقت استخدام كلمة مرور تقليدية، يمكنك تعيين واحدة بسهولة عبر رابط استعادة كلمة المرور.
                </div>

                <div style='text-align: center; margin: 32px 0;'>
                    <a href='{SystemConstants.BaseUrl}/auth/forgot-password' class='email-btn' style='background: linear-gradient(135deg, #0058BE 0%, #091426 100%); color: #ffffff; padding: 14px 32px; border-radius: 12px; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block; box-shadow: 0 4px 14px rgba(0, 88, 190, 0.3); max-width: 100%; box-sizing: border-box; word-break: break-word;'>
                        تعيين كلمة مرور
                    </a>
                </div>
                
                <p class='email-text' style='color: #75777D; font-size: 13px; line-height: 1.8; margin: 0;'>إذا لم تكن ترغب في إضافة كلمة مرور، يمكنك تجاهل هذه الخطوة والاستمرار في استخدام حساب جوجل لتسجيل الدخول بشكل طبيعي.</p>");
        }

        private static string BuildSectionTitle(string title)
        {
            return $"<h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>{WebUtility.HtmlEncode(title)}</h2>";
        }

        private static string BuildAlertCard(string message, string type = "info")
        {
            var (bgColor, borderColor, textColor, icon) = type.ToLowerInvariant() switch
            {
                "success" => ("#E6F6F4", "#00A292", "#006B60", "✅ "),
                "error" => ("#FFEDEC", "#BA1A1A", "#BA1A1A", "❌ "),
                "warning" => ("#FFFBEB", "#F59E0B", "#B45309", "⚠️ "),
                _ => ("#F8F9FF", "#D3E4FE", "#0058BE", "💡 ") // info
            };

            return $@"
                <div class='email-card-box' style='background-color: {bgColor}; border: 1px solid {borderColor}; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: {textColor}; font-size: 15px; font-weight: 700; text-align: center; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    {icon}{message}
                </div>";
        }

        private static string BuildInfoTable(IEnumerable<(string Label, string Value)> details)
        {
            var encodedDetails = details.Select((detail, index) =>
                $"<tr style='border-bottom: 1px solid #E2E8F0;{(index % 2 == 1 ? " background-color: #F8F9FF;" : " background-color: #FFFFFF;")}'>" +
                $"<td class='info-table-label' style='padding: 14px 20px; color: #75777D; font-weight: 600; width: 40%; word-break: break-word;'>{WebUtility.HtmlEncode(detail.Label)}:</td>" +
                $"<td style='padding: 14px 20px; color: #0B1C30; font-weight: 700; word-break: break-word;'>{WebUtility.HtmlEncode(detail.Value)}</td></tr>");

            return $@"
                <div class='email-card-box' style='background-color: #F8F9FF; border: 1px solid #C5C6CD; border-radius: 16px; overflow: hidden; margin: 24px 0; box-sizing: border-box; max-width: 100%;'>
                    <table class='info-table' role='presentation' cellSpacing='0' cellPadding='0' border='0' width='100%' style='border-collapse: collapse; font-size: 15px; width: 100%;'>
                        {string.Concat(encodedDetails)}
                    </table>
                </div>";
        }

        private static string BuildPrimaryButton(string text, string url)
        {
            return $@"
                <div style='text-align: center; margin: 32px 0;'>
                    <a href='{WebUtility.HtmlEncode(url)}' class='email-btn' style='background: linear-gradient(135deg, {ButtonColor} 0%, {PrimaryColor} 100%); color: #ffffff; padding: 14px 32px; border-radius: 12px; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block; box-shadow: 0 4px 14px rgba(0, 88, 190, 0.3); max-width: 100%; box-sizing: border-box; word-break: break-word;'>
                        {WebUtility.HtmlEncode(text)}
                    </a>
                </div>";
        }

        public static string BuildUrgentCaseNotificationEmailTemplate(
            string caseName,
            string caseCode,
            int age,
            Gender gender,
            string government,
            string city,
            DateTime publishedAt,
            string detailsUrl)
        {
            var details = new[]
            {
                ("اسم الحالة", caseName),
                ("العمر", $"{age} سنة"),
                ("النوع", gender == Gender.Male ? "ذكر" : "أنثى"),
                ("المحافظة", government),
                ("المدينة", city),
                ("وقت النشر", publishedAt.ToLocalTime().ToString("yyyy/MM/dd hh:mm tt")),
                ("كود الحالة", caseCode)
            };

            var header = $@"
                <div style='text-align: center; margin-bottom: 20px;'>
                    <span style='font-size: 48px; display: block; margin-bottom: 8px;'>🚨</span>
                    <h2 class='email-h2' style='color: #BA1A1A; font-size: 24px; font-weight: 800; margin: 0;'>تنبيه حالة عاجلة!</h2>
                </div>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0; text-align: center;'>يرجى الانتباه، تم الإبلاغ عن حالة عاجلة بالقرب منك. كل ثانية تصنع فارقاً.</p>";

            var footer = "<p class='email-text' style='color: #75777D; font-size: 13px; line-height: 1.8; margin: 0; text-align: center;'>وصلك هذا التنبيه بناءً على إعدادات موقعك. يمكنك تعديل الإعدادات من المنصة.</p>";

            return BuildCaseEmailTemplate(
                heading: string.Empty,
                details: details,
                detailsUrl: detailsUrl,
                alertMessage: "هذه الحالة تم تصنيفها كحالة عاجلة، يرجى المساعدة في حال توافر أي معلومات.",
                alertType: "warning",
                buttonText: "عرض تفاصيل الحالة والمساعدة",
                customHeaderHtml: header,
                customFooterHtml: footer);
        }

        public static string BuildCaseApprovedEmailTemplate(
            string userName,
            string caseCode,
            string caseType,
            string detailsUrl)
        {
            var details = new[]
            {
                ("كود الحالة", caseCode),
                ("نوع الحالة", caseType)
            };

            var header = $@"
                <div style='text-align: center; margin-bottom: 20px;'>
                    <span style='font-size: 48px; display: block; margin-bottom: 8px;'>🎉</span>
                    <h2 class='email-h2' style='color: #091426; font-size: 24px; font-weight: 800; margin: 0;'>مرحباً {WebUtility.HtmlEncode(userName)}، تمت الموافقة!</h2>
                </div>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0; text-align: center;'>يسعدنا إخبارك بأنه تم مراجعة الحالة وقبولها بنجاح وهي الآن متاحة للبحث والمشاركة.</p>";

            return BuildCaseEmailTemplate(
                heading: string.Empty,
                details: details,
                detailsUrl: detailsUrl,
                alertMessage: "تمت الموافقة على حالتك بنجاح وتم نشرها على المنصة.",
                alertType: "success",
                buttonText: "عرض تفاصيل الحالة",
                customHeaderHtml: header);
        }

        public static string BuildCaseRejectedEmailTemplate(
            string userName,
            string caseCode,
            string rejectionReason,
            string detailsUrl)
        {
            var details = new[]
            {
                ("كود الحالة", caseCode),
                ("سبب الرفض", rejectionReason)
            };

            var header = $@"
                {BuildSectionTitle($"مرحباً، {userName}")}
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>لقد قام فريقنا بمراجعة الحالة، ووجدنا بعض الملاحظات التي تمنعنا من نشرها. يرجى الاطلاع على التفاصيل أدناه.</p>";

            return BuildCaseEmailTemplate(
                heading: string.Empty,
                details: details,
                detailsUrl: detailsUrl,
                alertMessage: "عذراً، لم نتمكن من الموافقة على الحالة المرفوعة في الوقت الحالي.",
                alertType: "error",
                buttonText: "فتح تفاصيل الحالة",
                customHeaderHtml: header);
        }

        private static string BuildCaseEmailTemplate(
            string heading,
            IEnumerable<(string Label, string Value)> details,
            string detailsUrl,
            string? alertMessage = null,
            string alertType = "info",
            string buttonText = "عرض تفاصيل الحالة",
            string? customHeaderHtml = null,
            string? customFooterHtml = null)
        {
            var body = $@"
                {customHeaderHtml}
                {(!string.IsNullOrEmpty(heading) ? BuildSectionTitle(heading) : string.Empty)}
                {(!string.IsNullOrEmpty(alertMessage) ? BuildAlertCard(alertMessage, alertType) : string.Empty)}
                {BuildInfoTable(details)}
                {BuildPrimaryButton(buttonText, detailsUrl)}
                {customFooterHtml}
            ";

            return WrapInBaseLayout(body);
        }

        public static string BuildComplaintResolvedTemplate(string fullName, string solutionMessage)
        {
            return WrapInBaseLayout($@"
                <h2 class='email-h2' style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                <p class='email-text' style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>يسعدنا إخبارك بأنه تمت مراجعة شكواك والرد عليها من قِبل فريق الدعم لدينا.</p>
                
                <div class='email-card-box' style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #006B60; border-right: 4px solid #00A292; box-sizing: border-box; max-width: 100%; word-break: break-word;'>
                    <div style='font-size: 15px; font-weight: 700; color: #00A292; margin-bottom: 8px;'>💡 رسالة الحل:</div>
                    <div style='font-size: 15px; line-height: 1.8; color: #0B1C30; word-break: break-word;'>
                        {solutionMessage}
                    </div>
                </div>
                
                <p class='email-text' style='color: #75777D; font-size: 14px; line-height: 1.8; margin: 0 0 24px 0;'>إذا كان لديك أي استفسار إضافي، يمكنك تقديم شكوى جديدة من خلال المنصة وسيتواصل معك فريقنا في أقرب وقت.</p>");
        }
    }
}
