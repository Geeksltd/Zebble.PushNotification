namespace Zebble.Plugin
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json.Linq;
    using Zebble;
    using System.Threading.Tasks;
    using Foundation;
    using UIKit;
    using Zebble.NativeImpl;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PushNotification : DevicePushNotification.INativeImplementation
    {
        public Task DoRegister()
        {
            var app = UIApplication.SharedApplication;

            if (Device.OS.IsAtLeastiOS(8, 0))
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

        public Task DoUnregister()
        {
            UIApplication.SharedApplication.UnregisterForRemoteNotifications();
            return OnUnregisteredSuccess();
        }

        public async Task<bool> OnMessageReceived(object message)
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

            if (Device.PushNotification.ReceivedMessage.IsHandled())
            {
                var notification = new NotificationMessage(values);
                await Device.PushNotification.ReceivedMessage.RaiseOn(Device.ThreadPool, notification);
            }
            else
            {
                var applicationName = NSBundle.MainBundle.InfoDictionary.ObjectForKey(NSObject.FromObject("CFBundleName")).ToString();
                await Device.LocalNotification.Show(applicationName, values["body"].Value<string>());
            }

            return true;
        }

        public async Task OnRegisteredSuccess(object token)
        {
            var cleanToken = (token as NSData)?.Description.OrEmpty().Trim().Remove(" ").Trim('<', '>').Trim();
            await Device.PushNotification.Registered.RaiseOn(Device.ThreadPool, cleanToken);
            SetUserDefault(cleanToken);
        }

        public async Task OnUnregisteredSuccess()
        {
            await Device.PushNotification.UnRegistered.RaiseOn(Device.ThreadPool);
            SetUserDefault(string.Empty);
        }

        void SetUserDefault(string token)
        {
            NSUserDefaults.StandardUserDefaults.SetString(token, "token");
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }
    }
}