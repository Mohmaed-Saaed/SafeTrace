namespace SafeTrace.Application.Constants
{
    public static class EmailTemplates
    {
        public static string BuildArabicOtpEmailTemplate(string fullName, string otpCode, string contextTitle, string contextualDescription)
        {
            return $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 550px; margin: 0 auto; padding: 30px; border: 1px solid #eef2f5; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.03); text-align: right;'>
                <div style='text-align: center; margin-bottom: 25px;'>
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
                    <h1 style='color: #2b5a8f; font-size: 28px; margin: 0; font-weight: 700;'>منصة SafeTrace</h1>
                    <p style='color: #8c9ba5; font-size: 13px; margin: 5px 0 0 0;'>نظام تتبع وإعادة المفقودين الذكي</p>
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
                    <span style='color: #2b5a8f;'>فريق عمل منصة SafeTrace</span>
                </p>

            </div>";
        }
    }
}