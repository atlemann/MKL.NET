﻿module Stats.EstimatorTests

open MKLNET
open CsCheck
open TestsCSharp
open MathNet.Numerics.Statistics

let quartile = test "quartile" {

    test "5_order" {
        let! xs = Gen.Double.OneTwo.Array.[5]
        let qe = QuartileEstimator()
        for x in xs do qe.Add x
        Check.greaterThanOrEqual qe.Q0 qe.Q1
        Check.greaterThanOrEqual qe.Q1 qe.Q2
        Check.greaterThanOrEqual qe.Q2 qe.Q3
        Check.greaterThanOrEqual qe.Q3 qe.Q4
    }

    test "vs_p2" {
        let! xs = Gen.Int.[-100, 100].Select(fun i -> float i * 0.1).Array.[5, 50]
        let expected = P2QuantileEstimator(0.5)
        let actual = QuartileEstimator()
        for x in xs do
            expected.AddValue x
            actual.Add x
        Check.close VeryHigh (expected.GetQuantile()) actual.Median
        Check.greaterThanOrEqual actual.Q0 actual.Q1
        Check.greaterThanOrEqual actual.Q1 actual.Q2
        Check.greaterThanOrEqual actual.Q2 actual.Q3
        Check.greaterThanOrEqual actual.Q3 actual.Q4
    }

    test "faster" {
        let! xs = Gen.Double.OneTwo.Array
        Check.faster
            (fun () ->
                let e = QuartileEstimator()
                for x in xs do e.Add x
            )
            (fun () ->
                let e = P2QuantileEstimator(0.5)
                for x in xs do e.AddValue x
            )
    }

    test "add_same" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let qe1 = QuartileEstimator()
        for x in xs1 do qe1.Add x
        let qe2 = QuartileEstimator()
        for x in xs2 do qe2.Add x
        let qe3 = qe1 + qe2
        qe1.Add qe2
        Check.equal qe3.N qe1.N
        Check.equal qe3.Q0 qe1.Q0
        Check.equal qe3.Q1 qe1.Q1
        Check.equal qe3.Q2 qe1.Q2
        Check.equal qe3.Q3 qe1.Q3
        Check.equal qe3.Q4 qe1.Q4
    }

    test "add" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let qe3 = QuartileEstimator()
        let qe1 = QuartileEstimator()
        for x in xs1 do
            qe1.Add x
            qe3.Add x
        let qe2 = QuartileEstimator()
        for x in xs2 do
            qe2.Add x
            qe3.Add x
        let qe4 = qe1 + qe2
        Check.equal qe3.N qe4.N
        Check.equal qe3.Q0 qe4.Q0
        Check.between (min qe1.Q1 qe2.Q1) (max qe1.Q1 qe2.Q1) qe4.Q1
        Check.between (min qe1.Q2 qe2.Q2) (max qe1.Q2 qe2.Q2) qe4.Q2
        Check.between (min qe1.Q3 qe2.Q3) (max qe1.Q3 qe2.Q3) qe4.Q3
        Check.equal qe3.Q4 qe4.Q4
    }
}

let quantile = test "quantile" {

    test "vs_p2" {
        let! xs = Gen.Int.[-100, 100].Select(fun i -> float i * 0.1).Array.[5, 50]
        let expected = P2QuantileEstimator(0.6)
        let actual = QuantileEstimator(0.6)
        for x in xs do
            expected.AddValue x
            actual.Add x
        Check.close VeryHigh (expected.GetQuantile()) actual.Quantile
    }

    test "faster" {
        let! xs = Gen.Double.OneTwo.Array
        Check.faster
            (fun () ->
                let e = QuantileEstimator(0.6)
                for x in xs do e.Add x
            )
            (fun () ->
                let e = P2QuantileEstimator(0.6)
                for x in xs do e.AddValue x
            )
    }

    test "add_same" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let qe1 = QuantileEstimator(0.6)
        for x in xs1 do qe1.Add x
        let qe2 = QuantileEstimator(0.6)
        for x in xs2 do qe2.Add x
        let qe3 = qe1 + qe2
        qe1.Add qe2
        Check.equal qe3.N qe1.N
        Check.equal qe3.Quantile qe1.Quantile
    }
}

let histogram = test "histogram" {

    test "vs_quartile" {
        let! xs = Gen.Int.[-100, 100].Select(fun i -> float i * 0.1).Array.[5, 50]
        let expected = QuartileEstimator()
        let actual = HistogramEstimator(5)
        for x in xs do
            expected.Add x
            actual.Add x
        Check.equal expected.N0 actual.N.[0]
        Check.equal expected.N1 actual.N.[1]
        Check.equal expected.N2 actual.N.[2]
        Check.equal expected.N3 actual.N.[3]
        Check.equal expected.N  actual.N.[4]
        Check.close VeryHigh expected.Q0 actual.Q.[0]
        Check.close VeryHigh expected.Q1 actual.Q.[1]
        Check.close VeryHigh expected.Q2 actual.Q.[2]
        Check.close VeryHigh expected.Q3 actual.Q.[3]
        Check.close VeryHigh expected.Q4 actual.Q.[4]
    }
}

let moment4 = test "moment4" {

    test "vs_mathnet" {
        let! xs = Gen.Double.[-1000.0, 1000.0].Array.[5, 500]
        let expected = RunningStatistics()
        let actual = Moment4Estimator()
        for x in xs do
            expected.Push x
            actual.Add x
        Check.close VeryHigh expected.Mean actual.Mean
        Check.close VeryHigh expected.Variance actual.Variance
        Check.close VeryHigh expected.StandardDeviation actual.StandardDeviation
        Check.close VeryHigh expected.Skewness actual.Skewness
        Check.close VeryHigh expected.Kurtosis actual.Kurtosis
    }

    test "faster" {
        let! xs = Gen.Double.OneTwo.Array
        Check.faster
            (fun () ->
                let e = Moment4Estimator()
                for x in xs do e.Add x
            |> repeat 300)
            (fun () ->
                let e = RunningStatistics()
                for x in xs do e.Push x
            |> repeat 300)
    }

    test "add_same" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let e1 = Moment4Estimator()
        for x in xs1 do e1.Add x
        let e2 = Moment4Estimator()
        for x in xs2 do e2.Add x
        let e3 = e1 + e2
        e1.Add e2
        Check.equal e3.N e1.N
        Check.equal e3.M1 e1.M1
        Check.equal e3.S2 e1.S2
        Check.equal e3.S3 e1.S3
        Check.equal e3.S4 e1.S4
    }

    test "add" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let qe3 = Moment4Estimator()
        let qe1 = Moment4Estimator()
        for x in xs1 do
            qe1.Add x
            qe3.Add x
        let qe2 = Moment4Estimator()
        for x in xs2 do
            qe2.Add x
            qe3.Add x
        let qe4 = qe1 + qe2
        Check.close VeryHigh qe4.Mean qe3.Mean
        Check.close VeryHigh qe4.Variance qe3.Variance
        Check.close VeryHigh qe4.StandardDeviation qe3.StandardDeviation
        Check.close VeryHigh qe4.Skewness qe3.Skewness
        Check.close VeryHigh qe4.Kurtosis qe3.Kurtosis
    }
}

let moment3 = test "moment3" {

    test "vs_mathnet" {
        let! xs = Gen.Double.[-1000.0, 1000.0].Array.[5, 500]
        let expected = RunningStatistics()
        let actual = Moment3Estimator()
        for x in xs do
            expected.Push x
            actual.Add x
        Check.close VeryHigh expected.Mean actual.Mean
        Check.close VeryHigh expected.Variance actual.Variance
        Check.close VeryHigh expected.StandardDeviation actual.StandardDeviation
        Check.close VeryHigh expected.Skewness actual.Skewness
    }

    test "faster" {
        let! xs = Gen.Double.OneTwo.Array
        Check.faster
            (fun () ->
                let e = Moment3Estimator()
                for x in xs do e.Add x
            |> repeat 100)
            (fun () ->
                let e = RunningStatistics()
                for x in xs do e.Push x
            |> repeat 100)
    }

    test "add_same" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let e1 = Moment3Estimator()
        for x in xs1 do e1.Add x
        let e2 = Moment3Estimator()
        for x in xs2 do e2.Add x
        let e3 = e1 + e2
        e1.Add e2
        Check.equal e3.N e1.N
        Check.equal e3.M1 e1.M1
        Check.equal e3.S2 e1.S2
        Check.equal e3.S3 e1.S3
    }

    test "add" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let qe3 = Moment3Estimator()
        let qe1 = Moment3Estimator()
        for x in xs1 do
            qe1.Add x
            qe3.Add x
        let qe2 = Moment3Estimator()
        for x in xs2 do
            qe2.Add x
            qe3.Add x
        let qe4 = qe1 + qe2
        Check.close VeryHigh qe4.Mean qe3.Mean
        Check.close VeryHigh qe4.Variance qe3.Variance
        Check.close VeryHigh qe4.StandardDeviation qe3.StandardDeviation
        Check.close VeryHigh qe4.Skewness qe3.Skewness
    }
}

let moment2 = test "moment2" {

    test "vs_mathnet" {
        let! xs = Gen.Double.[-1000.0, 1000.0].Array.[5, 500]
        let expected = RunningStatistics()
        let actual = Moment2Estimator()
        for x in xs do
            expected.Push x
            actual.Add x
        Check.close VeryHigh expected.Mean actual.Mean
        Check.close VeryHigh expected.Variance actual.Variance
        Check.close VeryHigh expected.StandardDeviation actual.StandardDeviation
    }

    test "faster" {
        let! xs = Gen.Double.OneTwo.Array
        Check.faster
            (fun () ->
                let e = Moment2Estimator()
                for x in xs do e.Add x
            |> repeat 100)
            (fun () ->
                let e = RunningStatistics()
                for x in xs do e.Push x
            |> repeat 100)
    }

    test "add_same" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let e1 = Moment2Estimator()
        for x in xs1 do e1.Add x
        let e2 = Moment2Estimator()
        for x in xs2 do e2.Add x
        let e3 = e1 + e2
        e1.Add e2
        Check.equal e3.N e1.N
        Check.equal e3.M1 e1.M1
        Check.equal e3.S2 e1.S2
    }

    test "add" {
        let! xs1 = Gen.Double.OneTwo.Array.[5, 50]
        and! xs2 = Gen.Double.OneTwo.Array.[5, 50]
        let qe3 = Moment2Estimator()
        let qe1 = Moment2Estimator()
        for x in xs1 do
            qe1.Add x
            qe3.Add x
        let qe2 = Moment2Estimator()
        for x in xs2 do
            qe2.Add x
            qe3.Add x
        let qe4 = qe1 + qe2
        Check.close VeryHigh qe4.Mean qe3.Mean
        Check.close VeryHigh qe4.Variance qe3.Variance
        Check.close VeryHigh qe4.StandardDeviation qe3.StandardDeviation
    }
}

let all =
    test "stats_estimator" {
        quartile
        quantile
        histogram
        moment4
        moment3
        moment2
    }