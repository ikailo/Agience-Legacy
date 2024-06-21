﻿using Agience.SDK;
using Agience.SDK.Models;
using Binance.Net.Clients;
using CryptoExchange.Net.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Plugins.Primary.Finance
{
    public class CryptoExchangeData : IAgiencePlugin
    {
        [KernelFunction, Description("Get the spot price of a Cryptocurrency from Binance.")]
        public async Task<decimal> GetCryptoTickerExchangeData(
            [Description("The specific ticker to retrieve the Cryptocurrency price")] string ticker)
        {
            //Provider: https://github.com/JKorf/Binance.Net
            var restClient = new BinanceRestClient();
            var tickerResult = await restClient.SpotApi.ExchangeData.GetTickerAsync(ticker);
            if (tickerResult.Success)
                return tickerResult.Data.LastPrice;
            else
                throw new Exception($"{tickerResult.ResponseStatusCode}: {tickerResult.Error}");
        }
    }
}
