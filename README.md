[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.PushNotification/master/Shared/NuGet/Icon.png "Zebble.PushNotification"


## Zebble.PushNotification

![logo]

A Zebble plugin that allow you to using push notification services.


[![NuGet](https://img.shields.io/nuget/v/Zebble.PushNotification.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.PushNotification/)

> Push Notification (PN) service is used to allow a server app to push notifications to mobile app installations. The server app will dispatch notifications to the mobile device indirectly, by sending it to a PN service:<br>
Apple: APNs (Apple Push Notification Service)<br>
Google: GCM (Google Cloud Messaging)<br>
Windows: WNS (Windows Notification Service)<br>

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.PushNotification/](https://www.nuget.org/packages/Zebble.PushNotification/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage

The Zebble mobile app registers for push notifications with the PN service.
PNs sends a unique token back to the app, which is the unique id of that installation.
The app will then register the token with the server app (via its API).
The server app will store this token as an App Installation Record in the database.
Normally the installation token is also associated to a USER record in cases where the mobile app user is registered.
Then later on, the server app can deliver notifications to each token (i.e. mobile app installation) via the PNs.

##### Notes

The message/payload (including json data) should not be more than  256 bytes for Apple,  2KB for Android, and  5KB for Windows.
Within the above limit, you can send and command readable by the mobile app.
 
#### Implementation in the App
The Zebble project template contains a file named `App.UI\PushNotificationListener.cs` All you need to do is fill in the blanks. It's fairly self explanatory.

### Platform Specific Notes

#### IOS

If you want your app to receive push notifications in the background (i.e. when it's not open on the device) then:

##### In `Info.plist` add the following:
```xml
<key>UIBackgroundModes</key>
<array>
    <string>remote-notification</string>
</array>
```
Also in Entitlements.plist add the following as the content of dict tag:
```xml
<key>aps-environment</key>
<string>development</string>
```
and set the correct CodesignProvision in the iOS project properties.

##### Bundle ID for using Apple Services:

For using apple services you need to register your application in Apple developer portal to have a Bundle ID. Follow the instructions in the following link:  
https://developer.apple.com/library/content/documentation/IDEs/Conceptual/AppDistributionGuide/MaintainingProfiles/MaintainingProfiles.html

##### APP ID vs Bundle ID:
An App ID is a two-part string used to identify one or more apps from a single development team. The string consists of a Team ID and a bundle ID search string, with a period (.) separating the two parts. The Team ID is supplied by Apple and is unique to a specific development team, while the bundle ID search string is supplied by you to match either the bundle ID of a single app or a set of bundle IDs for a group of your apps.

##### Get a certificate for Push Notifications service:

https://github.com/Redth/PushSharp/wiki/How-to-Configure-&-Send-Apple-Push-Notifications-using-PushSharp

Activate push certificates on the Apple Developer Portal.
The iOS Application Bundle identifier must be the same corresponding to the profile used for code signing the app.
Right click on your Zebble iOS project in Visual Studio
Go to `iOS Applciation` tab
Set `Identifier` field to the Bundle ID you set in the previous step
Now you should have a `.p12` file which can be used in your server side application to send push notifications.

##### Server app
Add the following to your web.config of your server application (API):
```xml
    <add key="PN.Apple.Environment" value="Sandbox"/>  <! -- use "Production" for live -->
    <add key="PN.Apple.CertificateFile" value="" />
    <add key="PN.Apple.CertificatePassword" value="" />
```
##### Background mode
To support the background mode,  go to the Properties section of your iOS project in Visual Studio. Under iOS Application tab, scroll down to the "Background Modes" section and enable the Remote notifications option, and RemoteNotifications.

If you face any problems, check out the helper class on content folder: 
`PushNotificationApplicationDelegate.txt`. In order to setup correctly.

### Android

##### Setting Up Firebase Cloud Messaging
Before you can use FCM services in your app, you must create a new project (or import an existing project) via the Firebase Console. Use the following steps to create a Firebase Cloud Messaging project for your app:

- Sign into the  Firebase Console with your Google account (i.e., your Gmail address) and click **CREATE NEW PROJECT**.

- In the **Create a project** dialog, enter the name of your project and click **CREATE PROJECT**. In the following example, a new project called **YourProjectName** is created.


- In the Firebase Console **Overview**, click **Add Firebase to your Android app**.


- In the next screen, enter the package name of your app. In this example, the package name is **com.xamarin.fcmexample**. This value must match the package name of your Android app. An app nickname can also be entered in the App nickname field:
Click **ADD APP**.


A Server API key and a Client ID are automatically generated for the app. This information is packaged in a `google-services.json` file that is automatically downloaded when you click **ADD APP**. Be sure to save this file in a safe place.

##### Set the Package Name
In Firebase Cloud Messaging, you specified a package name for the FCM-enabled app. This package name also serves as the application ID that is associated with the API key. Configure the app to use this package name:

- Open the properties for your project.

- In the Android Manifest page, set the package name.

##### Add the Zebble.PushNotification Package 

- In Visual Studio, right-click References > Manage NuGet Packages.

- Click the Browse tab and search for Zebble.PushNotification.

- Install this package into your project.

 
##### Add the Google Services JSON File
The next step is to add the `google-services.json` file to the root directory of your project:

- Copy `google-services.json` to the project folder.

- Add `google-services.json` to the app project (click Show All Files in the Solution Explorer, right click `google-services.json`, then select Include in Project).

- Select `google-services.json` in the Solution Explorer window.

- In the Properties pane, set the Build Action to GoogleServicesJson (if the GoogleServicesJson build action is not shown, save and close the Solution, then reopen it).

##### Server application 
Add the following to your **web.config** of your server application (API):
```xml
<add key="PN.Google.SenderId" value="Your_Sender_Id" />
<add key="PN.Google.AuthToken" value="Your_Server_Key" />
```
These two values will be avaialable in your project Settings > Cloud Messaging section of Firebase Console:
 
##### Add Permissions to the Android Manifest
An Android application must have the following permissions configured before it can receive notifications from Google Cloud Messaging:
```xml
<application android:label="Project Title">
<receiver android:name="com.google.firebase.iid.FirebaseInstanceIdInternalReceiver" android:exported="false" />
<receiver android:name="com.google.firebase.iid.FirebaseInstanceIdReceiver" android:exported="true" android:permission="com.google.android.c2dm.permission.SEND">
<intent-filter>
<action android:name="com.google.android.c2dm.intent.RECEIVE" />
<action android:name="com.google.android.c2dm.intent.REGISTRATION" />
<category android:name="Package Name" />
</intent-filter>
</receiver>
</application>
```

##### Important:
For push notification to work properly on Android devices, you need to Clean/Rebuild the Solution and then delete obj and bin folders inside the Android project.

#### UWP

Add the following to your web.config of your server application (API):
```xml
<add key="PN.Windows.PackageName" value="" />
<add key="PN.Windows.PackageSID" value="" />
<add key="PN.Windows.ClientSecret" value="" />
```