using System.Collections.Generic;
using System.Threading.Tasks;
using FarmMachine.Domain.Commands.Exchange;
using FarmMachine.ExchangeBroker.Exchanges;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FarmMachine.ExchangeBroker.CommandHandlers
{
  public class BuySellCommandHandler : IConsumer<BuyCurrency>, IConsumer<SellCurrency>
  {
    public IMongoCollection<BsonDocument> _protocol;
    private IBittrexExchange _exchange;
    
    public BuySellCommandHandler(IMongoDatabase database, IBittrexExchange exchange)
    {
      _exchange = exchange;
      _protocol = database.GetCollection<BsonDocument>("protocol");
    }
    
    public async Task Consume(ConsumeContext<BuyCurrency> context)
    {
      var amount = _exchange.RiskManager.GetActualAmount();
      
      await _protocol.InsertOneAsync(new BsonDocument(new Dictionary<string, object>
      {
        { "_id", context.Message.Id },
        { "amount", amount },
        { "bid", context.Message.Bid },
        { "timestamp", context.Message.Created },
        { "type", "buy" }
      }));
    }

    public async Task Consume(ConsumeContext<SellCurrency> context)
    {
      await _protocol.InsertOneAsync(new BsonDocument(new Dictionary<string, object>
      {
        { "_id", context.Message.Id },
        { "amount", context.Message.Amount },
        { "ask", context.Message.Ask },
        { "timestamp", context.Message.Created },
        { "type", "sell" }
      }));
    }
  }
}