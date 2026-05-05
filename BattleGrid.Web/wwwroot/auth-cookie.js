window.bgAuth = {
    read: async function () {
        const response = await fetch("/auth/session/read", {
            method: "GET",
            credentials: "include"
        });

        if (!response.ok || response.status === 204) {
            return null;
        }

        return await response.json();
    },

    store: async function (state) {
        await fetch("/auth/session/store", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify(state)
        });
    },

    clear: async function () {
        await fetch("/auth/session/clear", {
            method: "POST",
            credentials: "include"
        });
    }
};
