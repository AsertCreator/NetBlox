window.netblox = {
    getCookie: (name) => {
        let matches = document.cookie.match(new RegExp(
            "(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"
        ));
        return matches ? decodeURIComponent(matches[1]) : undefined;
    },
    getSha256: async (text) => {
        const msgBuffer = new TextEncoder().encode(message);\
        const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
        return hashHex;
    },
    getPublicServiceApiUrl: () => "http://" + window.location.hostname + "/api";
    LoginService: {
        hasLoggedIn: () => window.netblox.getCookie("nblogtok") == undefined,
        login: async (uname, passw) => {
            if (this.hasLoggedIn())
                this.logout();
            const phash = getSha256(passw);
            const resp = await fetch(window.netblox.getPublicServiceApiUrl() + "/user/login", {
                method: 'POST',
                headers: {
                    'Content-Type': 'text/plain;charset=utf-8'
                },
                body: `${uname}\n${phash}`
            });
            if (resp.ok) {
                console.log("logged in successfully!");
                document.cookie = "nblogtok=" + resp.text() + "; maxage=" + 60 * 60 * 24 * 30; // for a month
                return true;
            }
            else {
                console.error("failed to log in!");
                return false;
            }
        },
        logout: () => {
            if (this.hasLoggedIn())
                window.cookie = "nblogtok=";
            console.log("logged out!");
        }
    },
    QueryService: {
        getGameInfo: () =>  
            
        }
    }
};