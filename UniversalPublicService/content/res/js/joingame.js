const main = document.querySelector('#main-content');
const root = ReactDOM.createRoot(main);
const e = React.createElement;

class JoinGamePage extends React.Component {
	constructor(props) {
		super(props);
	}

	render() {
		return (<div>
			<h1>Game Join Helper</h1>
			<button onClick={x => {
				window.netblox.JoinService.joinGame(0, 0)
			}}>Random Game</button>
			<button onClick={x => {
				window.netblox.JoinService.forceJoinGame({
					"e": true,
					"g": "efrjiverwyu"
				})
			}}>Start with invalid data</button>
		</div>);
	}
}

root.render(<JoinGamePage />);