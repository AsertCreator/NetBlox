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
			<p>Would you like to join this community?</p>
			<p>
				<input id="usern" placeholder="Username"></input>
			</p>
			<p>
				<input type="password" id="password" placeholder="Password"></input>
			</p>
			<p>
				<input type="password" id="rpassword" placeholder="Repeat your password"></input>
			</p>
			<p>
				<button onClick={x => {
					const result = document.querySelector("#result");
					const usern = document.querySelector("#usern").value;
					const password = document.querySelector("#password").value;
					const rpassword = document.querySelector("#rpassword").value;

					if (password != rpassword) {
						result.innerText = "Password do not match!";
						result.style.color = "red";
					}
					else {
						result.innerText = "Registering...";
						result.style.color = "white";

						window.netblox.LoginService.register(usern, password).then(x => {
							if (!(x[0])) {
								result.innerText = x[1];
								result.style.color = "red";
							} else {
								result.innerText = "Success!";
								result.style.color = "green";
								window.location.href = "/";
							}
						});
					}
				}}>Login</button>
			</p>
			<p>
				<span id="result"></span>
			</p>
		</div>);
	}
}

root.render(<SearchPage />);