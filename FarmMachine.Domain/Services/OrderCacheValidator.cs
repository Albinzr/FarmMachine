using System;
using System.Collections.Generic;
using System.Linq;
using FarmMachine.Domain.Models;
using Serilog;

namespace FarmMachine.Domain.Services
{
  public class OrderCacheValidator
  {
    public event EventHandler<OrderEventBacktest> DetectOrder;
    private Dictionary<DateTime, OrderEventBacktest> _cache { get; set; }

    public OrderCacheValidator()
    {
      _cache = new Dictionary<DateTime, OrderEventBacktest>();
    }

    public ValidationResult Push(List<BacktestOrderPair> pairs)
    {
      Log.Information("Push validation orders");
      
      var listNew = new List<OrderEventBacktest>();
      var timeNow = DateTime.Now;
      
      var result = new ValidationResult();
      var orders = pairs.Select(x => x.Right).Where(x => x != null && x.DateTime.Month == timeNow.Month).OrderBy(x => x.DateTime).Select(x => new OrderEventBacktest
      {
        EventType = x.EventType,
        Price = x.Price,
        DateTime = x.DateTime
      }).ToList();

      foreach (var order in orders)
      {
        if (!_cache.ContainsKey(order.DateTime))
        {
          _cache.Add(order.DateTime, order);
          
          listNew.Add(order);
        }
//        else if (order.DateTime.Hour != timeNow.Hour && order.DateTime.Day != timeNow.Day)
//        {
//          _cache.Add(order.DateTime, order);
//        }
      }

      result.DetectedCount = listNew.Count;
      result.Order = null;

      if (listNew.Count != 0)
      {
        var targetOrder = listNew.LastOrDefault();
        
        result.Order = targetOrder;
        var resultTime = timeNow - targetOrder.DateTime;
        
        Log.Information($"Detected new order from backtest. Time spread: {resultTime}");

        if (resultTime.TotalMinutes <= 15)
        {
          Log.Information($"Order need push");
          DetectOrder?.Invoke(this, targetOrder);
          
          result.Status = ValidationStatus.Pushed;
        }
        else
        {
          result.Status = ValidationStatus.Reload;
          
          Log.Information($"Order not equals by time. Need hour: [{timeNow}], fact: [{targetOrder.DateTime.Hour}]");
          
//          _cache.Add(targetOrder.DateTime, targetOrder);

//          listNew.Remove(targetOrder);
//          foreach (var newOrder in listNew)
//          {
//            _cache.Remove(newOrder.DateTime);
//          }
        }
      }
      else
      {
        Log.Information("Not found order from backtest");
        
        result.Status = ValidationStatus.NotFound;
      }

      Cleanup();

      return result;
    }

    private void Cleanup()
    {
      var borderDateTime = DateTime.Now.AddDays(-5);

      for (var i = 0; i < _cache.Count; i++)
      {
        var key = _cache.Keys.ToArray()[i];
        var value = _cache.Values.ToArray()[i];
        var order = value;

        if (order.DateTime <= borderDateTime)
        {
          _cache.Remove(key);
        }
      }
    }
  }
}