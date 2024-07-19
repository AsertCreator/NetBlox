const main = document.querySelector('#main-content');
const root = ReactDOM.createRoot(main);
const e = React.createElement;

class SearchPage extends React.Component {
	constructor(props) {
		super(props);
		this.state = {};
	}

	render() {
		return (<div>
			<h1>Platform-wide search</h1>
			<input id="search-bar"></input>
			<button onClick={x => {
				const query = document.querySelector("#search-bar").value;
				window.netblox.QueryService.search(query).then(res =>
				{
					if (res.success) {
						var cards = [];
						for (const i in res.entries) {
							const x = res.entries[i];
							if (x.type == 1)
								cards.push(<MediumGameIcon
									key={i} gameid={x.id} name={x.name} author={"by " + x.authorname}
									icon="/res/img/defaultPlace.png" />)
							else
								cards.push(<MediumUserIcon
									key={i} userid={x.id} name={x.name} presence={x.presence}
									icon="/res/img/defaultPlace.png" />)
						}
						this.setState({ entries: cards });
					}
				});
			}}>Search</button>
			<p>
				{this.state.entries == undefined ? ("") : (<span>Found {this.state.entries.length} entries!<br/>{this.state.entries}</span>)}
			</p>
		</div>);
	}
}

root.render(<SearchPage />);