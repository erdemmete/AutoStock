self.addEventListener("push", event => {
    const payload = parsePayload(event);

    event.waitUntil((async () => {
        const visibleClient = await getVisibleClient();

        if (visibleClient) {
            visibleClient.postMessage({
                type: "sente360-push",
                payload
            });
            return;
        }

        await self.registration.showNotification(payload.title || "Sente360", {
            body: payload.body || "Yeni bildiriminiz var.",
            tag: payload.tag || `notification-${payload.notificationId || Date.now()}`,
            data: {
                url: payload.url || "/Notifications",
                notificationId: payload.notificationId || null
            },
            icon: "/icons/icon-192.svg",
            badge: "/icons/icon-192.svg"
        });
    })());
});

self.addEventListener("notificationclick", event => {
    event.notification.close();

    const targetUrl = new URL(event.notification.data?.url || "/Notifications", self.location.origin).href;

    event.waitUntil((async () => {
        const clientList = await clients.matchAll({
            type: "window",
            includeUncontrolled: true
        });

        for (const client of clientList) {
            if (new URL(client.url).origin === self.location.origin) {
                await client.focus();
                client.postMessage({
                    type: "sente360-push-click",
                    url: targetUrl,
                    notificationId: event.notification.data?.notificationId || null
                });
                return;
            }
        }

        await clients.openWindow(targetUrl);
    })());
});

function parsePayload(event) {
    if (!event.data) {
        return {
            title: "Sente360",
            body: "Yeni bildiriminiz var.",
            url: "/Notifications"
        };
    }

    try {
        return event.data.json();
    } catch {
        return {
            title: "Sente360",
            body: event.data.text() || "Yeni bildiriminiz var.",
            url: "/Notifications"
        };
    }
}

async function getVisibleClient() {
    const clientList = await clients.matchAll({
        type: "window",
        includeUncontrolled: true
    });

    return clientList.find(client =>
        new URL(client.url).origin === self.location.origin &&
        client.visibilityState === "visible"
    );
}
