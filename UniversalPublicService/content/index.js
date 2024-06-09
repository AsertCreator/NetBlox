const main = document.querySelector('#main-content');
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
		return (<a className="medium-game-icon" href={"/game/"+this.gameid}>
			<img src={this.icon}/>
			<span className="primary">{this.name}</span>
			<span className="secondary">{this.author}</span>
		</a>);
	}
}
class HomePage extends React.Component {
	constructor(props) {
		super(props);
	}
	
	render() {
		var cards = [];
		for (var i = 0; i < 50; i++){
			cards.push(<MediumGameIcon key={i} gameid={1000 + i} name={"Crossroads " + i} author="by @NetBlox" icon="https://tr.rbxcdn.com/9371f94cb8bf0b4bbae69398d3c59c99/150/150/Image/Webp"/>)
		}
		return (<div>
			<h1>Home</h1>
			{cards}
		</div>);
	}
}

root.render(<HomePage/>);