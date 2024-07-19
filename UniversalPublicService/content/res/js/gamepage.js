const main = document.querySelector('#main-content');
const root = ReactDOM.createRoot(main);
const e = React.createElement;

// we determine what game we are even looking at
var gid = Number.parseInt(window.location.pathname.substring(6));
debugger;

netblox.QueryService.getPlaceInfo(gid).then(x => {
	class GameOverviewPage extends React.Component {
		constructor(props) {
			super(props);
		}

		render() {
			return (<div>
				<h1>{x.name}</h1>
				<b>by {x.authorname}</b>
				<p>{x.desc}</p>
				<button onClick={x => {
					window.netblox.JoinService.joinGame(gid, 0)
				}}>Play</button>
			</div>);
		}
	}

	root.render(<GameOverviewPage />);

	document.querySelector("title").innerText = "NetBlox - " + x.name;
});
