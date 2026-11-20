# Trading and Economy

## Root trading and commerce

Root `packets/world/trade.js` can find an online character and send a trade request. Acceptance begins a trade and stores peer socket references, but the offered-item response uses a large filler packet and the final two-party settlement is commented out. The cancel branch logs without shown state cleanup. There is no completed atomic item/currency exchange.

`packets/world/trader.js` and `openshop.js` contain NPC/personal shop paths with incomplete validation. The comments identify missing amount checks and possible silver overflow. `itemactions.js` contains inventory/silver operations with partial bounds/stack logic.

`FruitSalad/Processes/Zone/Packets/trade.js` is a separate, more developed implementation: it records offers and sends inventory windows, but contains TODOs around trade state, amounts and pets/bottles. It does not establish complete safe settlement parity. Personal-shop and AP-shop code are also separate partial paths.

The account/character schemas store silver, stacked silver, storage silver and bank silver, but schema fields do not establish transaction correctness.

## .NET status

No trade, shop, buy/sell, AP, item transformation, or economy operation has been migrated. GP-021, GP-028, GP-033 and GP-034 remain `NOT_STARTED`. Future implementations must validate ownership at confirmation, coordinate both sides without holding locks over network/database I/O, and prove no item/currency duplication on cancellation, disconnect, concurrent confirmation or persistence failure. MongoDB transactions are not assumed.
