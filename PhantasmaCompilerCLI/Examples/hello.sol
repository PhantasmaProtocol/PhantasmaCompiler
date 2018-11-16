pragma solidity ^0.4.22;

contract MyContract {
	function Main (string operation, int a, int b) public pure returns (int) {
		if (operation == "add") return a + b;
		else
		if (operation == "sub") return a - b;
		if (operation == "mul") return a * b;
		return -1;		
	}
}