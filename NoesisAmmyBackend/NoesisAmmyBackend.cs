﻿namespace NoesisAmmyBackend
{
	#region

	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Ammy;
	using Ammy.BackendCommon;

	#endregion

	public class NoesisAmmyBackend : IAmmyBackend
	{
		public NoesisAmmyBackend()
		{
			Host.InitializeHost(this);
		}

		public IAmmyCompiler Compiler { get; set; }

		public string[] DefaultNamespaces => new[] { "Noesis" };

		public bool NeedRuntimeUpdate => false;

		public TypeNames TypeNames => NoesisTypeNames.Instance;

		public Type[] ProvideTypes()
		{
			return AppDomain.CurrentDomain
			                .GetAssemblies()
			                .SelectMany(a => a.GetTypes())
			                .ToArray();
		}

		public void TriggerCompilation(string rootPath, IReadOnlyList<string> sourceFilePaths, string outputDataPath)
		{
			var sources = sourceFilePaths.Select(
				                             path => (Source)new FileSource(path))
			                             .ToArray();

			var compilationRequest = new CompilationRequest(sources);

			var result = this.Compiler.Compile(compilationRequest);
			if (result.IsSuccess)
			{
				this.GenerateFiles(result.Files, rootPath, outputDataPath);
			}

			var messages = result.CompilerMessages;
			if (messages.Any(m => m.Type == CompilerMessageType.Error))
			{
				throw new Exception(
					"Ammy compilation errors: "
					+ string.Join(Environment.NewLine, messages.Select(FormatMessage)));
			}
		}

		private static string FormatMessage(CompilerMessage m)
		{
			return $"[{m.Type}] {m.Message}{Environment.NewLine}    at {m.Location.Filename}:{m.Location.Column}";
		}

		private void GenerateFiles(OutputFile[] files, string rootPath, string outputDataPath)
		{
			if (Directory.Exists(outputDataPath))
			{
				Directory.Delete(outputDataPath, recursive: true);
			}

			Directory.CreateDirectory(outputDataPath);

			foreach (var file in files)
			{
				var path = file.Filename;
				var localFilePath = path.Substring(rootPath.Length, rootPath.Length - ".ammy".Length)
				                    + ".xaml";

				var outputFilePath = Path.Combine(outputDataPath, localFilePath);
				File.WriteAllText(outputFilePath, file.Xaml);
			}
		}
	}
}