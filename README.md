# xst-dotnet

A .NET client for the Stealth (XST) daemon. One `netstandard2.0` assembly, so the
same build loads in Unity, on .NET Framework and on .NET 6 and newer.

103 of the daemon's 129 RPC methods have typed wrappers. The rest go through
`InvokeAsync`, which hands back the raw result node.

| What | Version |
|---|---|
| Target | `netstandard2.0` - Unity Engine, .NET Framework 4.6.1+, .NET 6/8/9 |
| Newtonsoft.Json | 13.0.3 |
| StealthCoind | v3.3.5.0 |

## Usage

One `XstClient` per daemon. It is thread safe and holds one pooled `HttpClient`,
so keep it for the life of the app and never build one per call.

    using var client = new XstClient("127.0.0.1", 46502, "rpcuser", "rpcpassword");

    decimal balance = await client.GetBalanceAsync();
    string  address = await client.GetNewAddressAsync();

A feeless send, with an OP_RETURN payload attached:

    string txid = await client.SendToAddressAsync(
        address, 1.5m, feeless: true, hexData: new[] { "aabbcc" });

Anything not wrapped:

    JToken info = await client.InvokeAsync("getstakerpriceinfo");

Failures arrive as exceptions:

| Exception | Means |
|---|---|
| `XstRpcException` | the daemon refused, carries its own error code |
| `XstAuthenticationException` | bad `rpcuser` or `rpcpassword` |

## Amounts

XST has 6 decimal places and a supply needing 14 significant digits. A `float`
holds about 7, and Newtonsoft parses fractional JSON numbers into `double` by
default, which rounds amounts before you can pick a type. This client forces
`FloatParseHandling.Decimal` at the reader, so what you get back is what the
daemon sent.

## Daemon setup

`StealthCoin.conf` needs:

    rpcuser=yourusername
    rpcpassword=alongrandompassword
    rpcallowip=127.0.0.1
    rpcbind=127.0.0.1
    rpcport=46502

## Build

    dotnet build
    dotnet test

Unit tests run against an in-process fake daemon, so no node, wallet or coins
are needed.

Integration tests skip themselves unless credentials are in the environment, so
they never break a fresh clone. Point them at a node and they run:

    XST_RPC_HOST=127.0.0.1        # default
    XST_RPC_PORT=46502            # default
    XST_RPC_USER=yourusername     # required
    XST_RPC_PASSWORD=yourpassword # required
    XST_RPC_EXPLORE=1             # daemon runs the explore API
    XST_RPC_ALLOW_SPEND=1         # permit tests that move coins, testnet only

Every model here was written by reading the daemon C++ source rather than by
watching live replies, so the integration suite carries a schema guard. It
compares each live reply against the model and fails on any field the model does
not map, which catches a wrong guess and later daemon drift. Spending tests
refuse to run unless `getinfo` reports testnet.

## Coverage

Typed wrappers cover node, chain, wallet, transactions, sending, stealth
addresses, the address index and rich list, extended keys, raw transactions,
maintenance and network statistics.

The 26 unwrapped calls are left alone on purpose - vestigial proof-of-work mining
calls, node control (`stop`, `sendalert`), and the qPoS staker write operations
(`purchasestaker`, `setstakerowner` and friends), which move a lot of value and
are for staker operators.
