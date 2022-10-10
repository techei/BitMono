﻿using BitMono.Core.Configuration.Extensions;
using BitMono.Core.Models;
using BitMono.GUI.API;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace BitMono.GUI.Data
{
	public class StoringProtections : IStoringProtections
	{
		private readonly IConfiguration m_Configuration;

		public StoringProtections(IConfiguration configuration)
		{
			m_Configuration = configuration;
			Protections = m_Configuration.GetProtectionSettings().ToList();
		}


		public IList<ProtectionSettings> Protections { get; }
	}
}