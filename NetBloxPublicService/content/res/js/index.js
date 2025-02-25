const main = document.querySelector('#main-content');
const root = ReactDOM.createRoot(main);
const e = React.createElement;

class HomePage extends React.Component {
	constructor(props) {
		super(props);
		this.state = {};
	}

	componentDidMount() {
		var cards = [];
		var proms = [];
		for (var i = 0; i < 1; i++) {
			const prom = netblox.QueryService.getPlaceInfo(i);
			proms.push(prom);
			prom.then(x => {
				cards.push(<MediumGameIcon
					key={i} gameid={x.id} name={x.name} author={"by " + x.authorname}
					icon="/res/img/defaultPlace.png" />);
			});
		}

		Promise.all(proms).then(x => {
			this.setState(Object.assign(this.state, { cards: cards }));
		});

		netblox.LoginService.getSelf().then(x => {
			if (x != null)
				this.setState(Object.assign(this.state, { username: x.name }));
		});
	}
	
	render() {
		return (<div>
			<h1>{this.state.username == undefined ? "Home" : "Welcome back, " + this.state.username}</h1>
			{this.state.cards == undefined ? (<p>Loading...</p>) : this.state.cards}
		</div>);
	}
}

root.render(<HomePage/>);