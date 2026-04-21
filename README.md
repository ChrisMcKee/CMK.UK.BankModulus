# CMK.UK.BankModulus

## A .NET library for validating bank account numbers using modulus checks

This library provides functionality to validate UK bank account numbers using modulus checks, which are commonly used in banking systems to ensure the integrity of account numbers.

### Data Source

* Vocalink https://www.vocalink.com/tools/modulus-checking/

### Usage

To use the library, you can call the static `BankValidator.Validate` method

```csharp
using CMK.UK.BankModulus;

BankValidator.Validate("089999", "66374958")
```

You can also pass in the `Tables` object if you want to use a custom set of modulus check tables:

```csharp
BankValidation.Validate(sortCode, accountNumber, MyTablesObj)
```

You can construct a `Tables` object using the embedded valacdos/subs files or by loading your own.

```csharp
var embeddedTables = Tables.FromEmbedded();
```

or if you want to control the data source (e.g. load from a file):

```csharp
var myValacdosData = File.ReadAllText("path/to/valacdos.txt");
var mySubsData = File.ReadAllText("path/to/subs.txt");
var myTables = Tables.Parse(myValacdosData, mySubsData); // returns parsed tables assuming your data is correct
```

