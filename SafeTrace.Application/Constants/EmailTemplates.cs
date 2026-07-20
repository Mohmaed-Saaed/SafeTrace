using SafeTrace.Application.Helpers;
using SafeTrace.Application.Constants;

namespace SafeTrace.Application.Constants
{
    public static class EmailTemplates
    {
        public static string BuildArabicOtpEmailTemplate(string fullName, string otpCode, string contextTitle, string contextualDescription)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                    <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                 alt='لقاء Logo'
                 style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                <p style='color: #475569; font-size: 15px; line-height: 1.6; margin-bottom: 30px;'>{contextualDescription}</p>
                
                <div style='background: #f8fafc; border: 1px dashed #cbd5e1; padding: 20px; border-radius: 10px; text-align: center; margin-bottom: 30px;'>
                    <span style='font-size: 12px; color: #64748b; display: block; margin-bottom: 8px; font-weight: 500;'>{contextTitle}</span>
                    <span style='font-size: 34px; font-weight: 700; color: #2563eb; letter-spacing: 8px; font-family: monospace; display: inline-block;'>{otpCode}</span>
                </div>
                
                <p style='color: #ef4444; font-size: 13px; font-weight: 500; margin-bottom: 25px;'>تنبيه أمني: تنتهي صلاحية هذا الرمز بعد 10 دقائق تلقائياً. يرجى عدم مشاركة هذا الرمز مع أي شخص كائن من كان لحماية سرية حسابك.</p>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <p style='color: #94a3b8; font-size: 12px; line-height: 1.5; margin: 0;'>إذا لم تقم بإنشاء هذا الحساب أو تقديم هذا الطلب، يمكنك تجاهل هذا البريد الإلكتروني بأمان دون اتخاذ أي إجراء إضافي.</p>
                <br/>
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildArabicNewMessageEmailTemplate(
    string receiverName,
    string senderName,
    string messagePreview,
    string chatLink)
        {
            return $@"
    <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>

        <div style='text-align: center; margin-bottom: 25px;'>
            
            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                 alt='لقاء Logo'
                 style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />

            <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700;'>
                منصة لقاء
            </h1>

            <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0;'>
                نظام تتبع وإعادة المفقودين الذكي
            </p>

        </div>

        <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />

        <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>
            مرحباً {receiverName}
        </h2>

        <p style='color: #475569; font-size: 15px; line-height: 1.7;'>
            لديك رسالة جديدة من <strong>{senderName}</strong>
        </p>

        <div style='background: #f8fafc; border: 1px solid #e2e8f0; padding: 15px; border-radius: 10px; margin: 20px 0; color: #334155; font-size: 14px;'>
            {messagePreview}
        </div>

        <div style='text-align: center; margin: 30px 0;'>
            <a href='{chatLink}'
               style='background: #2563eb; color: #ffffff; padding: 12px 22px; border-radius: 8px; text-decoration: none; font-weight: 600; display: inline-block;'>
                عرض الرسالة
            </a>
        </div>

        <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px;' />

        <p style='color: #94a3b8; font-size: 12px; line-height: 1.5; margin: 0;'>
            إذا لم تكن تتوقع هذه الرسالة، يمكنك تجاهل هذا البريد بأمان.
        </p>

        <br/>

        <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>
            مع خالص التحية،<br/>
            <span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span>
        </p>

    </div>";
        }

        public static string BuildAdminRegisteredTemplate(string fullName, string email, string password, string role)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                    <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                    alt='لقاء Logo'
                    style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>تم إنشاء حساب جديد لك في منصة لقاء من قِبل الإدارة. يمكنك الآن تسجيل الدخول والمشاركة معنا باستخدام البيانات التالية:</p>
                
                <div style='background: #f8fafc; border: 1px dashed #cbd5e1; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                    <p style='margin: 8px 0; color: #334155;'><strong>البريد الإلكتروني:</strong> <span dir='ltr' style='color: #2563eb;'>{email}</span></p>
                    <p style='margin: 8px 0; color: #334155;'><strong>كلمة المرور المؤقتة:</strong> <span dir='ltr' style='color: #2563eb; font-family: monospace; font-size: 16px;'>{password}</span></p>
                    <p style='margin: 8px 0; color: #334155;'><strong>الدور الممنوح:</strong> {TranslateRoleToArabicHelper.TranslateRoleToArabic(role)}</p>
                </div>
                
                <p style='color: #ef4444; font-size: 13px; font-weight: 600; margin-bottom: 25px;'>ملاحظة هامة: نوصي بشدة بتغيير كلمة المرور المؤقتة فور تسجيل دخولك لأول مرة لضمان أمان حسابك.</p>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildRoleChangedTemplate(string fullName, string newRole)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                    <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                    alt='لقاء Logo'
                    style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>نعلمك بأنه قد تم تحديث الصلاحية الإدارية (الدور) الخاص بحسابك في منصة لقاء بواسطة الإدارة.</p>
                <div style='background: #f0fdf4; border: 1px dashed #bbf7d0; padding: 15px; border-radius: 10px; margin: 20px 0; color: #166534; font-size: 16px; font-weight: 600; text-align: center;'>
                    دورك الجديد هو: {TranslateRoleToArabicHelper.TranslateRoleToArabic(newRole)}
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px; margin-top: 25px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildPermissionsChangedTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                   <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                    alt='لقاء Logo'
                    style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>نعلمك بأنه قد تم تحديث <strong>الصلاحيات الفردية (الاستثنائية)</strong> الممنوحة لحسابك في منصة لقاء بواسطة الإدارة.</p>
                
                <div style='background: #f8fafc; border: 1px dashed #cbd5e1; padding: 15px; border-radius: 10px; margin: 20px 0;'>
                    <p style='color: #64748b; font-size: 14px; margin: 0; text-align: center;'>تم تخصيص هذه الصلاحيات لتتناسب مع مهامك الحالية في النظام.</p>
                </div>
                
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px; margin-top: 25px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildVerificationApprovedTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                    <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                    alt='لقاء Logo'
                    style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>تهانينا يا {fullName}!</h2>
                <div style='text-align: center; margin: 20px 0;'>
                    <span style='font-size: 50px;'>🎉</span>
                </div>
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>يسعدنا إخبارك بأنه تمت مراجعة صورة هويتك وقبولها بنجاح.</p>
                <div style='background: #f0fdf4; border: 1px dashed #bbf7d0; padding: 15px; border-radius: 10px; margin: 20px 0; color: #166534; font-size: 15px;'>
                    حسابك الآن موثق بالكامل ويمتلك صلاحيات <strong>مستخدم موثق</strong>، مما يتيح لك الاستفادة من ميزات إضافية مثل إضافة الحالات طويلة المدى وحالات مجهولي الهوية.
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px; margin-top: 25px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildVerificationRejectedTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                     alt='لقاء Logo'
                     style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                
                <div style='background: #fef2f2; border: 1px dashed #fecaca; padding: 15px; border-radius: 10px; margin: 20px 0; color: #991b1b; font-size: 15px; text-align: center;'>
                    عذراً، لم نتمكن من قبول صورة إثبات الهوية التي قمت برفعها.
                </div>
                
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>أسباب الرفض الشائعة تشمل: عدم وضوح الصورة، أو عدم وضوح البيانات، أو عدم تصوير الوجه الأمامي للبطاقة.</p>
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>يرجى تسجيل الدخول إلى حسابك، والتوجه إلى الإعدادات، وإعادة رفع صورة واضحة ومقروءة (للوجه الأمامي) لبطاقة الهوية ليتمكن فريقنا من توثيق حسابك بنجاح.</p>
                
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px; margin-top: 25px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildBlockStatusChangedTemplate(string fullName, bool isBlocked)
        {
            string status = isBlocked ? "حظر" : "إلغاء الحظر عن";
            string message = isBlocked
                ? "تم تعليق حسابك في منصة لقاء. يرجى التواصل معنا إذا كنت تعتقد أن هذا حدث عن طريق الخطأ."
                : "تم تفعيل حسابك مرة أخرى في منصة لقاء. يمكنك الآن تسجيل الدخول والمشاركة.";
            
            string bgClass = isBlocked ? "background: #fef2f2; border: 1px dashed #fecaca; color: #991b1b;" : "background: #f0fdf4; border: 1px dashed #bbf7d0; color: #166534;";

            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                     alt='لقاء Logo'
                     style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>تم تحديث حالة حسابك في منصة لقاء بواسطة الإدارة.</p>
                
                <div style='{bgClass} padding: 15px; border-radius: 10px; margin: 20px 0; font-weight: bold; font-size: 16px; text-align: center;'>
                    الإجراء: {status} الحساب
                </div>
                
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>{message}</p>
                
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px; margin-top: 25px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildLoginAlertTemplate(string fullName, string ipAddress, string browser, string os)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                 alt='لقاء Logo'
                 style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>تنبيه أمني: تسجيل دخول جديد</h2>
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>مرحباً {fullName}، تم تسجيل دخول جديد إلى حسابك في منصة لقاء بالتفاصيل التالية:</p>
                
                <div style='background: #f8fafc; border: 1px dashed #cbd5e1; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                    <p style='margin: 8px 0; color: #334155;'><strong>IP:</strong> <span dir='ltr' style='color: #2563eb;'>{ipAddress}</span></p>
                    <p style='margin: 8px 0; color: #334155;'><strong>Browser:</strong> <span dir='ltr' style='color: #2563eb;'>{browser}</span></p>
                    <p style='margin: 8px 0; color: #334155;'><strong>OS:</strong> <span dir='ltr' style='color: #2563eb;'>{os}</span></p>
                </div>
                
                <p style='color: #ef4444; font-size: 14px; font-weight: 600; text-align: center;'>إذا لم تكن أنت من قام بتسجيل الدخول، يرجى تغيير كلمة المرور فوراً لحماية حسابك.</p>
                
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px; margin-top: 25px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق الأمان - منصة لقاء</span></p>
            </div>";
        }

        public static string BuildPasswordResetSuccessTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                    <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                     alt='لقاء Logo'
                     style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                
                <div style='background: #f0fdf4; border: 1px dashed #bbf7d0; padding: 20px; border-radius: 10px; margin: 20px 0; color: #166534; text-align: center;'>
                    <span style='font-size: 24px; display: block; margin-bottom: 10px;'>✅</span>
                    <strong style='font-size: 16px;'>تم إعادة تعيين / تغيير كلمة المرور الخاصة بحسابك بنجاح.</strong>
                </div>
                
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>تم تسجيل الخروج من كافة الأجهزة الأخرى لضمان أمان حسابك.</p>
                <p style='color: #ef4444; font-size: 14px; font-weight: 600;'>إذا لم تقم بهذا الإجراء، يرجى <strong>تقديم شكوى من خلال المنصة</strong> فوراً ليتمكن فريقنا من التدخل.</p>
                
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px; margin-top: 25px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildEmailConfirmedSuccessTemplate(string fullName)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                     alt='لقاء Logo'
                     style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700; letter-spacing: -0.5px;'>منصة لقاء</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0; text-transform: uppercase;'>نظام تتبع وإعادة المفقودين الذكي</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
                <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
                
                <div style='background: #f0fdf4; border: 1px dashed #bbf7d0; padding: 20px; border-radius: 10px; margin: 20px 0; color: #166534; text-align: center;'>
                    <span style='font-size: 24px; display: block; margin-bottom: 10px;'>✉️✅</span>
                    <strong style='font-size: 16px;'>تم تأكيد عنوان بريدك الإلكتروني بنجاح!</strong>
                </div>
                
                <p style='color: #475569; font-size: 15px; line-height: 1.6;'>شكراً لك على تأكيد بريدك الإلكتروني. يمكنك الآن الاستفادة من كافة خدمات وميزات منصة لقاء بكامل طاقتها.</p>
                
                <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 20px; margin-top: 25px;' />
                <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
            </div>";
        }

        public static string BuildUrgentCaseNotificationEmailTemplate(
            string receiverName,
            string caseCode,
            int age,
            string government,
            string city,
            string detailsUrl)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,.05); text-align: right;'>

                <div style='text-align:center;margin-bottom:25px;'>
                <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                     alt='لقاء Logo'
                     style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
                    <h1 style='margin:0;color:#2b5a8f;font-size:28px;font-weight:700;'>
                        منصة لقاء
                    </h1>

                    <p style='margin-top:8px;color:#94a3b8;font-size:13px;'>
                        نظام تتبع وإعادة المفقودين الذكي
                    </p>
                </div>

                <hr style='border:none;border-top:1px solid #eef2f5;margin:25px 0;'>

                <h2 style='margin-top:0;color:#1e293b;'>
                    مرحباً {receiverName} 👋
                </h2>

                <p style='font-size:15px;color:#475569;line-height:1.8;'>

                    تم تسجيل <strong>حالة عاجلة</strong> بالقرب من موقعك،
                    وقد تحتاج إلى الاطلاع على تفاصيلها.

                </p>

                <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:20px;margin:25px 0;'>

                    <table style='width:100%;border-collapse:collapse;font-size:15px;'>

                        <tr>
                            <td style='padding:8px 0;color:#64748b;'>كود الحالة</td>
                            <td style='padding:8px 0;font-weight:bold;color:#0f172a;'>{caseCode}</td>
                        </tr>

                        <tr>
                            <td style='padding:8px 0;color:#64748b;'>العمر</td>
                            <td style='padding:8px 0;font-weight:bold;color:#0f172a;'>{age} سنة</td>
                        </tr>

                        <tr>
                            <td style='padding:8px 0;color:#64748b;'>المحافظة</td>
                            <td style='padding:8px 0;font-weight:bold;color:#0f172a;'>{government}</td>
                        </tr>

                        <tr>
                            <td style='padding:8px 0;color:#64748b;'>المدينة</td>
                            <td style='padding:8px 0;font-weight:bold;color:#0f172a;'>{city}</td>
                        </tr>

                    </table>

                </div>

                <div style='text-align:center;margin:35px 0;'>

                    <a href='{detailsUrl}'
                    style='background:#2563eb;
                            color:white;
                            padding:14px 28px;
                            border-radius:8px;
                            text-decoration:none;
                            font-size:16px;
                            font-weight:600;
                            display:inline-block;'>

                        عرض تفاصيل الحالة

                    </a>

                </div>

                <p style='font-size:13px;color:#64748b;line-height:1.8;'>

                    إذا لم يعمل الزر يمكنك استخدام الرابط التالي:

                </p>

                <p style='word-break:break-all;'>

                    <a href='{detailsUrl}'>{detailsUrl}</a>

                </p>

                <hr style='border:none;border-top:1px solid #eef2f5;margin:25px 0;'>

                <p style='font-size:12px;color:#94a3b8;line-height:1.7;'>

                    تم إرسال هذا البريد لأن موقعك الحالي أو موقع منزلك يقع ضمن نطاق الحالة.
                    إذا كنت لا ترغب في استلام هذه التنبيهات يمكنك تعديل إعدادات الإشعارات من داخل التطبيق.

                </p>

                <br/>

                <p style='font-size:14px;color:#475569;font-weight:600;'>

                    مع خالص التحية ❤️<br/>

                    <span style='color:#2b5a8f;'>فريق لقاء</span>

                </p>

            </div>";
        }


        public static string BuildComplaintResolvedTemplate(string fullName, string solutionMessage)
        {
            return $@"
    <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
        <div style='text-align: center; margin-bottom: 25px;'>
            <img src='{SystemConstants.BaseUrl}/Images/logo.jpg'
                 alt='لقاء Logo'
                 style='width: 80px; height: 80px; object-fit: contain; margin-bottom: 10px;' />
            <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700;'>منصة لقاء</h1>
            <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0;'>نظام تتبع وإعادة المفقودين الذكي</p>
        </div>
        <hr style='border: 0; border-top: 1px solid #f0f4f8; margin-bottom: 25px;' />
        <h2 style='color: #1e293b; font-size: 20px; margin-top: 0; font-weight: 600;'>مرحباً، {fullName}</h2>
        <p style='color: #475569; font-size: 15px; line-height: 1.6;'>يسعدنا إخبارك بأنه تمت مراجعة شكواك والرد عليها من قِبل فريق الدعم لدينا.</p>
        <div style='background: #f0fdf4; border: 1px dashed #bbf7d0; padding: 20px; border-radius: 10px; margin: 20px 0; color: #166534;'>
            <strong>رسالة الحل:</strong><br/><br/>
            {solutionMessage}
        </div>
        <p style='color: #475569; font-size: 14px; line-height: 1.6;'>إذا كان لديك أي استفسار إضافي، يمكنك تقديم شكوى جديدة من خلال المنصة وسيتواصل معك فريقنا في أقرب وقت.</p>
        <hr style='border: 0; border-top: 1px solid #f0f4f8; margin: 20px 0;' />
        <p style='color: #475569; font-size: 14px; margin: 0; font-weight: 600;'>مع خالص التحية،<br/><span style='color: #2b5a8f;'>فريق عمل منصة لقاء</span></p>
    </div>";
        }
    }


}