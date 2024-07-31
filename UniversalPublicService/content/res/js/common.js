window.netblox = {
	getCookie: (name) => {
		let matches = document.cookie.match(new RegExp(
			"(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"
		));
		return matches ? decodeURIComponent(matches[1]) : undefined;
	},
	getSha256: async (text) => {
		const msgBuffer = new TextEncoder().encode(text);
		const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer);
		const hashArray = Array.from(new Uint8Array(hashBuffer));
		const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
		return hashHex;
	},
	getPublicServiceApiUrl: () => "http://" + window.location.hostname + "/api",
	LoginService: {
		hasLoggedIn: () => window.netblox.getCookie("nblogtok") == undefined,
		login: async (uname, passw) => {
			if (window.netblox.LoginService.hasLoggedIn())
				window.netblox.LoginService.logout();
			const phash = await window.netblox.getSha256(passw);
			const resp = await fetch(window.netblox.getPublicServiceApiUrl() + "/users/login?name=" +
				encodeURIComponent(uname) + "&phash=" + encodeURIComponent(phash), {
				method: 'GET',
				headers: {
					'Content-Type': 'text/plain;charset=utf-8'
				},
			});
			if (resp.ok) {
				console.log("logged in successfully!");
				document.cookie = "nblogtok=" + encodeURIComponent((await resp.json()).token) + "; maxage=" + 60 * 60 * 24 * 30; // for a month
				return true;
			}
			else {
				console.error("failed to log in!");
				return false;
			}
		},
		logout: () => {
			if (window.netblox.LoginService.hasLoggedIn())
				document.cookie = "nblogtok=; expires=Thu, 01 Jan 1970 00:00:01 GMT";
			console.log("logged out!");
		}
	},
	QueryService: {
		getPlaceInfo: async (gameid) => {
			let resp = await fetch("http://" + window.location.hostname + "/api/places/info?id=" + encodeURIComponent(gameid), {
				method: 'GET',
				headers: {
					'Content-Type': 'text/plain;charset=utf-8'
				},
			});

			let json = await resp.json();
			return json;
		},
		search: async (query) => {
			let resp = await fetch("http://" + window.location.hostname + "/api/search?q=" + encodeURIComponent(query) + "&amount=100", {
				method: 'GET',
				headers: {
					'Content-Type': 'text/plain;charset=utf-8'
				},
			});

			let json = await resp.json();
			return json;
		},
		getOnlineMode: (n) => {
			switch (n) {
				case 0:
					return "Offline";
				case 1:
					return "On website";
				case 2:
					return "On website";
				case 3:
					return "Playing";
				case 4:
					return "In Studio";
				case 5:
					return "Banned";
			}
		}
	},
	JoinService: {
		joinGame: async (gameid, servid) => {
			let resp = await fetch("http://" + window.location.hostname + "/api/places/join?id=" + encodeURIComponent(gameid)
				+ "&sid=" + encodeURIComponent(servid), {
				method: 'GET',
				headers: {
					'Content-Type': 'text/plain;charset=utf-8'
				}
			});
			if (resp.ok) {
				let json = await resp.json();
				const ip = json.ip;
				const port = json.port;
				window.netblox.JoinService.forceJoinGame({
					"e": !window.netblox.LoginService.hasLoggedIn(),
					"g": ip + ":" + port
				});
			} else {
				debugger;
				throw "Could not join the game, code " + resp.status;
			}
		},
		forceJoinGame: async (args) => {
			window.location.href = "netblox-client://base64 " + btoa(JSON.stringify(args));
		}
	}
};

const main = document.querySelector('header');
const root = ReactDOM.createRoot(main);
const e = React.createElement;

class MediumGameIcon extends React.Component {
	constructor(props) {
		super(props);
		this.name = props.name;
		this.author = props.author;
		this.icon = props.icon;
		this.gameid = props.gameid;
	}

	render() {
		return (<a className="medium-game-icon" href={"/game/" + this.gameid}>
			<img src={this.icon} />
			<span className="primary">{this.name}</span>
			<span className="secondary">{this.author}</span>
		</a>);
	}
}
class MediumUserIcon extends React.Component {
	constructor(props) {
		super(props);
		this.name = props.name;
		this.presence = props.presence;
		this.userid = props.userid;
		this.icon = props.icon;
	}

	render() {
		return (<a className="medium-user-icon" href={"/user/" + this.userid}>
			<img src={this.icon} />
			<span className="primary">{this.name}</span>
			<span className="secondary">{window.netblox.QueryService.getOnlineMode(this.presence)}</span>
		</a>);
	}
}
class Titlebar extends React.Component {
	constructor(props) {
		super(props);
	}

	render() {
		return (<div id="header-content">
			<a href="/" style={(window.location.pathname == "/" ? { textDecoration: 'underline' } : {})}>Welcome to NetBlox!</a>
			<a href="/join" style={(window.location.pathname == "/join" ? { textDecoration: 'underline' } : {})}>Join a game</a>
			<a href="/search" style={(window.location.pathname == "/search" ? { textDecoration: 'underline' } : {})}>Search</a>
			<div className="right-align">
				{
					window.netblox.LoginService.hasLoggedIn() ?
					(
						<a href="/" onClick={() => {
							window.netblox.LoginService.logout();
						}}>Logout</a>
					) :
					(
						<a href="/login" style={(window.location.pathname == "/login" ? { textDecoration: 'underline' } : {})}>Login</a>
					) // what the fuck visual studio. its re****ed isnt it?
				}
			</div>
		</div>);
	}
}

root.render(<Titlebar />);