pragma solidity ^0.4.22;

contract MyContract {
	function Add (int a, int b) public pure returns (int) {
		return a + b;
	}
	
	function Subtract(int a, int b) public pure returns (int) {
		return a - b;
	}
	
	function Multiply(int a, int b) public pure returns (int) {
		return a * b;
	}
}