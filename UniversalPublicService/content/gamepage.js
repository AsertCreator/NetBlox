const main = document.querySelector('#main-content');
const root = ReactDOM.createRoot(main);
const e = React.createElement;

async function joinGame() {
}
async function getGameName(gameid) {
}

// we determine what game we are even looking at
var gid = Number.parseInt(window.location.pathname.substring(6));
debugger;
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
