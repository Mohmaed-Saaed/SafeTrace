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

        public static string BuildArabicOtpEmailTemplate(string fullName, string otpCode, string contextTitle, string contextualDescription)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>
                    
                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 28px 0;'>{contextualDescription}</p>
                        
                        <div style='background-color: #F8F9FF; border: 2px dashed #0058BE; padding: 24px 20px; border-radius: 18px; text-align: center; margin: 28px 0;'>
                            <span style='font-size: 13px; color: #0058BE; display: block; margin-bottom: 10px; font-weight: 700;'>{contextTitle}</span>
                            <span style='font-size: 36px; font-weight: 800; color: #0058BE; letter-spacing: 10px; font-family: monospace; display: inline-block;'>{otpCode}</span>
                        </div>
                        
                        <div style='background-color: #FFFBEB; border: 1px solid #F59E0B; padding: 16px 20px; border-radius: 14px; margin-bottom: 24px;'>
                            <p style='color: #B45309; font-size: 13px; font-weight: 600; margin: 0; line-height: 1.7;'>⚠️ <strong>تنبيه أمني:</strong> تنتهي صلاحية هذا الرمز بعد 10 دقائق تلقائياً. يرجى عدم مشاركة هذا الرمز مع أي شخص كائن من كان لحماية سرية حسابك.</p>
                        </div>
                        
                        <p style='color: #75777D; font-size: 13px; line-height: 1.8; margin: 0;'>إذا لم تقم بإنشاء هذا الحساب أو تقديم هذا الطلب، يمكنك تجاهل هذا البريد الإلكتروني بأمان دون اتخاذ أي إجراء إضافي.</p>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildArabicNewMessageEmailTemplate(
            string receiverName,
            string senderName,
            string messagePreview,
            string chatLink)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً {receiverName}</h2>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>لديك رسالة جديدة من <strong style='color: #0058BE;'>{senderName}</strong></p>

                        <div style='background-color: #F8F9FF; border: 1px solid #D3E4FE; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #0B1C30; font-size: 15px; line-height: 1.8; border-right: 4px solid #0058BE;'>
                            <div style='font-weight: 700; color: #0058BE; margin-bottom: 8px; font-size: 13px;'>💬 معاينة الرسالة:</div>
                            {messagePreview}
                        </div>

                        <div style='text-align: center; margin: 32px 0;'>
                            <a href='{chatLink}' style='background: linear-gradient(135deg, #0058BE 0%, #091426 100%); color: #ffffff; padding: 14px 32px; border-radius: 12px; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block; box-shadow: 0 4px 14px rgba(0, 88, 190, 0.3);'>
                                عرض الرسالة
                            </a>
                        </div>

                        <p style='color: #75777D; font-size: 13px; line-height: 1.8; margin: 0;'>إذا لم تكن تتوقع هذه الرسالة، يمكنك تجاهل هذا البريد بأمان.</p>

                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildAdminRegisteredTemplate(string fullName, string email, string role)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>تم إنشاء حساب جديد لك في منصة لقاء من قِبل الإدارة. يمكنك الآن تفعيل حسابك عن طريق تعيين كلمة مرور جديدة من خلال الرابط أدناه:</p>
                        
                        <div style='background-color: #F8F9FF; border: 1px solid #C5C6CD; border-radius: 16px; overflow: hidden; margin: 24px 0;'>
                            <table role='presentation' cellSpacing='0' cellPadding='0' border='0' width='100%' style='border-collapse: collapse; font-size: 15px;'>
                                <tr style='border-bottom: 1px solid #E2E8F0;'>
                                    <td style='padding: 14px 20px; color: #75777D; font-weight: 600; width: 40%;'>البريد الإلكتروني:</td>
                                    <td style='padding: 14px 20px; color: #0058BE; font-weight: 700;' dir='ltr'>{email}</td>
                                </tr>
                                <tr style='background-color: #FFFFFF;'>
                                    <td style='padding: 14px 20px; color: #75777D; font-weight: 600;'>الدور الممنوح:</td>
                                    <td style='padding: 14px 20px; color: #0B1C30; font-weight: 700;'>{TranslateRoleToArabicHelper.TranslateRoleToArabic(role)}</td>
                                </tr>
                            </table>
                        </div>
                        
                        <div style='text-align: center; margin: 32px 0;'>
                            <a href='{SystemConstants.BaseUrl}/auth/forgot-password' style='background: linear-gradient(135deg, #0058BE 0%, #091426 100%); color: #ffffff; padding: 14px 32px; border-radius: 12px; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block; box-shadow: 0 4px 14px rgba(0, 88, 190, 0.3);'>
                                تعيين كلمة المرور
                            </a>
                        </div>

                        <div style='background-color: #FFFBEB; border: 1px solid #F59E0B; padding: 16px 20px; border-radius: 14px; margin-bottom: 24px;'>
                            <p style='color: #B45309; font-size: 13px; font-weight: 600; margin: 0; line-height: 1.7;'>📌 <strong>ملاحظة هامة:</strong> يجب تعيين كلمة المرور لتتمكن من تسجيل الدخول والمشاركة معنا.</p>
                        </div>

                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildRoleChangedTemplate(string fullName, string newRole)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>نعلمك بأنه قد تم تحديث الصلاحية الإدارية (الدور) الخاص بحسابك في منصة لقاء بواسطة الإدارة.</p>
                        
                        <div style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #006B60; font-size: 16px; font-weight: 700; text-align: center;'>
                            🛡️ دورك الجديد هو: <span style='color: #091426;'>{TranslateRoleToArabicHelper.TranslateRoleToArabic(newRole)}</span>
                        </div>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildPermissionsChangedTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>نعلمك بأنه قد تم تحديث <strong style='color: #0058BE;'>الصلاحيات الفردية (الاستثنائية)</strong> الممنوحة لحسابك في منصة لقاء بواسطة الإدارة.</p>
                        
                        <div style='background-color: #F8F9FF; border: 1px solid #D3E4FE; padding: 20px 24px; border-radius: 16px; margin: 24px 0;'>
                            <p style='color: #75777D; font-size: 14px; margin: 0; text-align: center; line-height: 1.7;'>🔐 تم تخصيص هذه الصلاحيات لتتناسب مع مهامك الحالية في النظام.</p>
                        </div>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildVerificationApprovedTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <div style='text-align: center; margin-bottom: 20px;'>
                            <span style='font-size: 48px; display: block; margin-bottom: 8px;'>🎉</span>
                            <h2 style='color: #091426; font-size: 24px; font-weight: 800; margin: 0;'>تهانينا يا {fullName}!</h2>
                        </div>
                        
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>يسعدنا إخبارك بأنه تمت مراجعة صورة هويتك وقبولها بنجاح.</p>
                        
                        <div style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #006B60; font-size: 15px; line-height: 1.8;'>
                            ✓ حسابك الآن موثق بالكامل ويمتلك صلاحيات <strong style='color: #091426;'>مستخدم موثق</strong>، مما يتيح لك الاستفادة من ميزات إضافية مثل إضافة الحالات طويلة المدى وحالات مجهولي الهوية.
                        </div>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildVerificationRejectedTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        
                        <div style='background-color: #FFEDEC; border: 1px solid #BA1A1A; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #BA1A1A; font-size: 15px; font-weight: 700; text-align: center;'>
                            ❌ عذراً، لم نتمكن من قبول صورة إثبات الهوية التي قمت برفعها.
                        </div>
                        
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 16px 0;'>أسباب الرفض الشائعة تشمل: عدم وضوح الصورة، أو عدم وضوح البيانات، أو عدم تصوير الوجه الأمامي للبطاقة.</p>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>يرجى تسجيل الدخول إلى حسابك، والتوجه إلى الإعدادات، وإعادة رفع صورة واضحة ومقروءة (للوجه الأمامي) لبطاقة الهوية ليتمكن فريقنا من توثيق حسابك بنجاح.</p>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildBlockStatusChangedTemplate(string fullName, bool isBlocked)
        {
            string status = isBlocked ? "حظر" : "إلغاء الحظر عن";
            string message = isBlocked
                ? "تم تعليق حسابك في منصة لقاء. يرجى التواصل معنا إذا كنت تعتقد أن هذا حدث عن طريق الخطأ."
                : "تم تفعيل حسابك مرة أخرى في منصة لقاء. يمكنك الآن تسجيل الدخول والمشاركة.";
            
            string bgClass = isBlocked ? "background-color: #FFEDEC; border: 1px solid #BA1A1A; color: #BA1A1A;" : "background-color: #E6F6F4; border: 1px solid #00A292; color: #006B60;";

            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>تم تحديث حالة حسابك في منصة لقاء بواسطة الإدارة.</p>
                        
                        <div style='{bgClass} padding: 20px 24px; border-radius: 16px; margin: 24px 0; font-weight: 700; font-size: 16px; text-align: center;'>
                            الإجراء: {status} الحساب
                        </div>
                        
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>{message}</p>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildLoginAlertTemplate(string fullName, string ipAddress, string browser, string os, string deviceName)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #BA1A1A; font-size: 22px; font-weight: 800; margin: 0 0 16px 0;'>🔔 تنبيه أمني: تسجيل دخول جديد</h2>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>مرحباً {fullName}، تم تسجيل دخول جديد إلى حسابك في منصة لقاء بالتفاصيل التالية:</p>
                        
                        <div style='background-color: #F8F9FF; border: 1px solid #C5C6CD; border-radius: 16px; overflow: hidden; margin: 24px 0;'>
                            <table role='presentation' cellSpacing='0' cellPadding='0' border='0' width='100%' style='border-collapse: collapse; font-size: 15px;'>
                                <tr style='border-bottom: 1px solid #E2E8F0;'>
                                    <td style='padding: 14px 20px; color: #75777D; font-weight: 600; width: 35%;'>IP:</td>
                                    <td style='padding: 14px 20px; color: #0058BE; font-weight: 700;' dir='ltr'>{ipAddress}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #E2E8F0; background-color: #FFFFFF;'>
                                    <td style='padding: 14px 20px; color: #75777D; font-weight: 600;'>Browser:</td>
                                    <td style='padding: 14px 20px; color: #0B1C30; font-weight: 700;' dir='ltr'>{browser}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #E2E8F0;'>
                                    <td style='padding: 14px 20px; color: #75777D; font-weight: 600;'>OS:</td>
                                    <td style='padding: 14px 20px; color: #0B1C30; font-weight: 700;' dir='ltr'>{os}</td>
                                </tr>
                                <tr style='background-color: #FFFFFF;'>
                                    <td style='padding: 14px 20px; color: #75777D; font-weight: 600;'>Device:</td>
                                    <td style='padding: 14px 20px; color: #0B1C30; font-weight: 700;' dir='ltr'>{deviceName}</td>
                                </tr>
                            </table>
                        </div>
                        
                        <div style='background-color: #FFEDEC; border: 1px solid #BA1A1A; padding: 16px 20px; border-radius: 14px; margin-bottom: 24px; text-align: center;'>
                            <p style='color: #BA1A1A; font-size: 14px; font-weight: 700; margin: 0;'>🚨 إذا لم تكن أنت من قام بتسجيل الدخول، يرجى تغيير كلمة المرور فوراً لحماية حسابك.</p>
                        </div>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق الأمان - منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildPasswordResetSuccessTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        
                        <div style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 24px 20px; border-radius: 16px; margin: 24px 0; color: #006B60; text-align: center;'>
                            <span style='font-size: 32px; display: block; margin-bottom: 10px;'>✅</span>
                            <strong style='font-size: 17px;'>تم إعادة تعيين / تغيير كلمة المرور الخاصة بحسابك بنجاح.</strong>
                        </div>
                        
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 16px 0;'>تم تسجيل الخروج من كافة الأجهزة الأخرى لضمان أمان حسابك.</p>
                        
                        <div style='background-color: #FFEDEC; border: 1px solid #BA1A1A; padding: 16px 20px; border-radius: 14px; margin-bottom: 24px;'>
                            <p style='color: #BA1A1A; font-size: 14px; font-weight: 600; margin: 0; line-height: 1.7;'>⚠️ إذا لم تقم بهذا الإجراء، يرجى <strong style='text-decoration: underline;'>تقديم شكوى من خلال المنصة</strong> فوراً ليتمكن فريقنا من التدخل.</p>
                        </div>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }

        public static string BuildEmailConfirmedSuccessTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        
                        <div style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 24px 20px; border-radius: 16px; margin: 24px 0; color: #006B60; text-align: center;'>
                            <span style='font-size: 32px; display: block; margin-bottom: 10px;'>✉️ ✅</span>
                            <strong style='font-size: 17px;'>تم تأكيد عنوان بريدك الإلكتروني بنجاح!</strong>
                        </div>
                        
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>شكراً لك على تأكيد بريدك الإلكتروني. يمكنك الآن الاستفادة من كافة خدمات وميزات منصة لقاء بكامل طاقتها.</p>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
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

            return BuildCaseEmailTemplate(string.Empty, details, detailsUrl, includeBranding: false);
        }

        public static string BuildCaseApprovedEmailTemplate(
            string userName,
            string caseCode,
            string caseType,
            string detailsUrl) =>
            BuildCaseEmailTemplate(
                $"مرحباً، {userName}",
                new[]
                {
                    ("نوع الحالة", caseType),
                    ("كود الحالة", caseCode),
                    ("التحديث", "تمت الموافقة على حالتك بنجاح."),
                    ("ملاحظة", "يمكنك البحث عن الحالة باستخدام كود الحالة أو متابعة تفاصيلها من خلال المنصة.")
                },
                detailsUrl);

        public static string BuildCaseRejectedEmailTemplate(
            string userName,
            string caseCode,
            string rejectionReason,
            string detailsUrl) =>
            BuildCaseEmailTemplate(
                $"مرحباً، {userName}",
                new[]
                {
                    ("كود الحالة", caseCode),
                    ("سبب الرفض", rejectionReason)
                },
                detailsUrl);

        private static string BuildCaseEmailTemplate(
            string heading,
            IEnumerable<(string Label, string Value)> details,
            string detailsUrl,
            bool includeBranding = true)
        {
            var encodedDetails = details.Select((detail, index) =>
                $"<tr style='border-bottom: 1px solid #E2E8F0;{(index % 2 == 1 ? " background-color: #F8F9FF;" : " background-color: #FFFFFF;")}'>" +
                $"<td style='padding:14px 20px;color:#75777D;font-weight:600;width:35%;'>{WebUtility.HtmlEncode(detail.Label)}</td>" +
                $"<td style='padding:14px 20px;font-weight:700;color:#0B1C30;'>{WebUtility.HtmlEncode(detail.Value)}</td></tr>");

            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    {(includeBranding ? $@"
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>{CompanyName}</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>" : string.Empty)}
                    
                    <div style='padding: 36px 32px;'>
                        {(!string.IsNullOrEmpty(heading) ? $"<h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 24px 0;'>{WebUtility.HtmlEncode(heading)}</h2>" : string.Empty)}
                        
                        <div style='background-color: #FFFFFF; border: 1px solid #C5C6CD; border-radius: 16px; overflow: hidden; margin: 24px 0;'>
                            <table role='presentation' cellSpacing='0' cellPadding='0' border='0' width='100%' style='width:100%;border-collapse:collapse;font-size:15px;'>
                                {string.Concat(encodedDetails)}
                            </table>
                        </div>

                        <div style='text-align:center;margin:32px 0;'>
                            <a href='{WebUtility.HtmlEncode(detailsUrl)}' style='background: linear-gradient(135deg, {ButtonColor} 0%, {PrimaryColor} 100%); color:#FFFFFF; padding:14px 32px; border-radius:12px; text-decoration:none; font-size:16px; font-weight:700; display:inline-block; box-shadow: 0 4px 14px rgba(0, 88, 190, 0.3);'>
                                عرض تفاصيل الحالة
                            </a>
                        </div>

                        {(includeBranding ? $@"
                        <hr style='border:0; border-top:1px solid #E2E8F0; margin:32px 0 24px 0;'/>
                        <p style='color:{SecondaryColor}; font-size:15px; font-weight:700; margin: 0;'>مع خالص التحية،<br/><span style='color:{PrimaryColor};'>{Footer}</span></p>" : string.Empty)}
                    </div>
                </div>
            </div>";
        }

        public static string BuildComplaintResolvedTemplate(string fullName, string solutionMessage)
        {
            return $@"
            <div dir='rtl' style='background-color: #F8F9FF; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif; padding: 40px 15px; width: 100%; box-sizing: border-box; text-align: right;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #FFFFFF; border-radius: 20px; border: 1px solid #C5C6CD; box-shadow: 0 10px 30px rgba(9, 20, 38, 0.05); overflow: hidden;'>
                    <div style='background: linear-gradient(135deg, #091426 0%, #0058BE 100%); padding: 36px 20px; text-align: center;'>
                        <div style='display: inline-block; background-color: #FFFFFF; width: 80px; height: 80px; border-radius: 50%; padding: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); margin-bottom: 12px;'>
                            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg' alt='لقاء Logo' style='width: 72px; height: 72px; border-radius: 50%; object-fit: cover; display: block; margin: 0 auto;' />
                        </div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 800; margin: 0 0 6px 0; letter-spacing: -0.5px; font-family: ""Cairo"", ""Segoe UI"", Tahoma, sans-serif;'>منصة لقاء</h1>
                        <p style='color: #D3E4FE; font-size: 13px; font-weight: 600; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>نظام تتبع وإعادة المفقودين الذكي</p>
                    </div>

                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #091426; font-size: 22px; font-weight: 700; margin: 0 0 16px 0;'>مرحباً، {fullName}</h2>
                        <p style='color: #0B1C30; font-size: 16px; line-height: 1.9; margin: 0 0 24px 0;'>يسعدنا إخبارك بأنه تمت مراجعة شكواك والرد عليها من قِبل فريق الدعم لدينا.</p>
                        
                        <div style='background-color: #E6F6F4; border: 1px solid #00A292; padding: 20px 24px; border-radius: 16px; margin: 24px 0; color: #006B60; border-right: 4px solid #00A292;'>
                            <div style='font-size: 15px; font-weight: 700; color: #00A292; margin-bottom: 8px;'>💡 رسالة الحل:</div>
                            <div style='font-size: 15px; line-height: 1.8; color: #0B1C30;'>
                                {solutionMessage}
                            </div>
                        </div>
                        
                        <p style='color: #75777D; font-size: 14px; line-height: 1.8; margin: 0 0 24px 0;'>إذا كان لديك أي استفسار إضافي، يمكنك تقديم شكوى جديدة من خلال المنصة وسيتواصل معك فريقنا في أقرب وقت.</p>
                        
                        <hr style='border: 0; border-top: 1px solid #E2E8F0; margin: 32px 0 24px 0;' />
                        <p style='color: #0B1C30; font-size: 15px; margin: 0; font-weight: 700;'>مع خالص التحية،<br/><span style='color: #0058BE;'>فريق عمل منصة لقاء</span></p>
                    </div>
                </div>
            </div>";
        }
    }
}
