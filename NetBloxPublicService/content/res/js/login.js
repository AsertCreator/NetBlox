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
			<h1>Welcome to NetBlox!</h1>
			<p>Would you like to login today?</p>
			<p>
				<input id="usern" placeholder="Username"></input>
			</p>
			<p>
				<input type="password" id="password" placeholder="Password"></input>
			</p>
			<p>
				<button onClick={x => {
					const result = document.querySelector("#result");
					const usern = document.querySelector("#usern").value;
					const password = document.querySelector("#password").value;

					result.innerText = "Logging in...";

					window.netblox.LoginService.login(usern, password).then(x => {
						if (!(x[0])) {
							result.innerText = x[1];
							result.style.color = "red";
						} else {
							result.innerText = "Success!";
							result.style.color = "green";
							window.location.href = "/";
						}
					});
				}}>Login</button>
			</p>
			<p>
				<span id="result"></span>
			</p>
		</div>);
	}
}

root.render(<SearchPage />);