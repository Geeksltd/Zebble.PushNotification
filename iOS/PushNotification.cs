namespace Zebble.Device
{
    using Foundation;
    using Newtonsoft.Json.Linq;
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using UIKit;
    using Olive;

    [EditorBrowsable(EditorBrowsableState.Never)]
    partial class PushNotification
    {
        static void Init()
        {
            UIRuntime.DidReceiveRemoteNotification += OnMessageReceived;
            UIRuntime.RegisteredForRemoteNotifications.Handle(OnRegisteredSuccess);
            UIRuntime.FailedToRegisterForRemoteNotifications.Handle((error) => ReceivedError.RaiseOn(Thread.Pool, error.LocalizedDescription));
        }

        static Task DoRegister()
        {
            var app = UIApplication.SharedApplication;

            if (OS.IsAtLeastiOS(8, 0))
            {
                app.RegisterUserNotificationSettings(
                    UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Alert |
                  UIUserNotificationType.Badge |
                  UIUserNotificationType.Sound, null));
                app.RegisterForRemoteNotifications();
            }
            else app.RegisterForRemoteNotificationTypes(UIRemoteNotificationType.Alert |
                UIRemoteNotificationType.Badge |
                UIRemoteNotificationType.Sound);

            return Task.CompletedTask;
        }

        static Task DoUnRegister()
        {
            UIApplication.SharedApplication.UnregisterForRemoteNotifications();
            return OnUnRegisteredSuccess();
        }

        static async Task<bool> OnMessageReceived(object message)
        {
            var userInfo = (NSDictionary)message;

            var values = new JObject();
            var keyAps = new NSString("aps");
            var keyAlert = new NSString("alert");

            if (userInfo.ContainsKey(keyAps))
                if (userInfo.ValueForKey(keyAps) is NSDictionary aps)
                    if (aps.ValueForKey(keyAlert) is NSDictionary alert)
                        foreach (var node in alert)
                            if (!values.TryGetValue(node.Key.ToString(), out var temp))
                                values.Add(node.Key.ToString(), node.Value.ToString());

            if (ReceivedMessage.IsHandled())
            {
                var notification = new Zebble.Device.LocalNotification.Notification
                {
                    Title = values["title"].ToString(),
                    Body = values["body"].ToString(),
                    NotifyTime = LocalTime.Now,
                };
                await ReceivedMessage.RaiseOn(Thread.Pool, notification);
            }
            else
            {
                var applicationName = NSBundle.MainBundle.InfoDictionary.ObjectForKey(NSObject.FromObject("CFBundleName")).ToString();
                await LocalNotification.Show(applicationName, values["body"].Value<string>());
            }

            return true;
        }

        static async Task OnRegisteredSuccess(NSData token)
        {
            byte[] result = new byte[token.Length];
            Marshal.Copy(token.Bytes, result, 0, (int)token.Length);
            var cleanToken = BitConverter.ToString(result).Replace("-", "");

            await Registered.RaiseOn(Thread.Pool, cleanToken);
            SetUserDefault(cleanToken);
        }

        static async Task OnUnRegisteredSuccess()
        {
            await UnRegistered.RaiseOn(Thread.Pool);
            SetUserDefault(string.Empty);
        }

        static void SetUserDefault(string token)
        {
            NSUserDefaults.StandardUserDefaults.SetString(token, "token");
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }
    }
}