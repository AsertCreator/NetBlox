const main = document.querySelector('#main-content');
const root = ReactDOM.createRoot(main);
const e = React.createElement;

function joinGame(gameid, servid) {
	var webs = new WebSocket("ws://" + window.location.host + ":443");
	webs.onopen = (e) => {
		webs.send(JSON.stringify({
			type: "joingame",
			gid: gameid,
			sid: servid
		}));
		webs.onmessage = (ev) => {
			var d = JSON.parse(ev.data);
			console.log("got server address");
			window.location.href = "netblox:--rbxl " + d.ip;
		};
	};
}

class JoinGamePage extends React.Component {
	constructor(props) {
		super(props);
	}

	render() {
		return (<div>
			<h1>Game Join Helper</h1>
			<button onClick={x => {
				joinGame(0, 0)
			}}>Random Game</button>
		</div>);
	}
}

root.render(<JoinGamePage />);