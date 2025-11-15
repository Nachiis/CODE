#include<iostream>
#include<string>
using std::cin;
using std::cout;
using std::string;

enum DFAState {
	Start,
	DigitBeforeDotAndE,
	DOT,
	E,
	AfterDotDigit,
	ESign,
	AfterEDigit,
	STATECOUNT,
};

bool isValidPascalNumber(const string& pascalNumber);

int main() {
	string pascalNumber;
	while (true) {
		cout << "Enter a Pascal unsigned number: ";
		if (!(cin >> pascalNumber)) {
			break;
		}
		if (isValidPascalNumber(pascalNumber)) {
			cout << "YES\n";
		}
		else {
			cout << "NO\n";
		}
	}
}

bool isValidPascalNumber(const string& pascalNumber)
{
	DFAState currentState = Start;
	for(char c:pascalNumber)
	{
		switch(currentState)
		{
			case Start:
				if(c>='0' && c<='9')
					currentState = DigitBeforeDotAndE;
				else
					return false;
				break;
			case DigitBeforeDotAndE:
				if(c>='0' && c<='9')
					currentState = DigitBeforeDotAndE;
				else if(c=='.')
					currentState = DOT;
				else if(c=='E' || c=='e')
					currentState = E;
				else
					return false;
				break;
			case DOT:
				if(c>='0' && c<='9')
					currentState = AfterDotDigit;
				else
					return false;
				break;
			case AfterDotDigit:
				if(c>='0' && c<='9')
					currentState = AfterDotDigit;
				else if(c=='E' || c=='e')
					currentState = E;
				else
					return false;
				break;
			case E:
				if(c=='+' || c=='-')
					currentState = ESign;
				else if(c>='0' && c<='9')
					currentState = AfterEDigit;
				else
					return false;
				break;
			case ESign:
				if(c>='0' && c<='9')
					currentState = AfterEDigit;
				else
					return false;
				break;
			case AfterEDigit:
				if(c>='0' && c<='9')
					currentState = AfterEDigit;
				else
					return false;
				break;
			default:
				return false;
		}
	}
	return true;
}


