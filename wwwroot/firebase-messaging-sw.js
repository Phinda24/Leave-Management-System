importScripts('https://www.gstatic.com/firebasejs/9.22.2/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.22.2/firebase-messaging-compat.js');

firebase.initializeApp({
    apiKey: "AIzaSyCBLH7BNFux4wjIf3Tw1WJ6xxk0nDXkb6U",
    authDomain: "leavemanagementsystem-9cd7d.firebaseapp.com",
    projectId: "leavemanagementsystem-9cd7d",
    messagingSenderId: "271028255699",
    appId: "1:271028255699:web:ce476aedaf8e5616e68d26"
});


const messaging = firebase.messaging();

messaging.onBackgroundMessage(function (payload) {
    const title = payload.notification?.title || 'Notification';
    const options = {
        body: payload.notification?.body,
        data: payload.data
    };
    self.registration.showNotification(title, options);
});
