const main = document.querySelector('#main-content');
const root = ReactDOM.createRoot(main);
const e = React.createElement;

async function joinGame(gameid, servid) {
	let resp = await fetch("http://" + window.location.hostname + "/api/joingame", {
		method: 'POST',
		headers: {
			'Content-Type': 'text/plain;charset=utf-8'
		},
		body: `${gameid}\n${servid}`
	});
	if (resp.ok) {
		let json = await resp.json();
		return json.name;
	} else {
		debugger;
		throw "Could not join the game, code " + resp.status;
	}
}
async function getGameName(gameid) {
	let resp = await fetch("http://" + window.location.hostname + "/api/places/name", {
		method: 'POST',
		headers: {
			'Content-Type': 'text/plain;charset=utf-8'
		},
		body: `${gameid}`
	});
	if (resp.ok) {
		let json = await resp.json();
		return json.name;
	} else {
		debugger;
		window.location.href = "http://" + window.location.hostname + "/";
		throw "Could not retrieve general game information, code " + resp.status;
	}
}

// we determine what game we are even looking at
var gid = Number.parseInt(window.location.pathname.substring(6));
getGameName(gid).then(x => {
	class GameOverviewPage extends React.Component {
		constructor(props) {
			super(props);
		}

		render() {
			return (<div>
				<h1>{x}</h1>
				<h3>by @NetBlox</h3>
				<button onClick={x => {
					joinGame(gid, 0)
				}}>Play</button>
			</div>);
		}
	}

	root.render(<GameOverviewPage />);
});
