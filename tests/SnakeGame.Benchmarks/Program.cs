using BenchmarkDotNet.Running;
using SnakeGame.Benchmarks;

// 性能基准测试入口
BenchmarkRunner.Run<GameEngineBenchmarks>();
BenchmarkRunner.Run<DataStructureBenchmarks>();