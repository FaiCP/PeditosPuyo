using FluentAssertions;
using PuyoDelivery.Core.Common;

namespace PuyoDelivery.Tests.Unit;

public class ResultTests
{
    [Fact]
    public void Success_Factory_SetsIsSuccess()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Code.Should().BeNull();
    }

    [Fact]
    public void Failure_Factory_SetsErrorAndCode()
    {
        var result = Result.Failure("Something failed", "ERR_01");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Something failed");
        result.Code.Should().Be("ERR_01");
    }

    [Fact]
    public void Failure_Factory_CodeIsOptional()
    {
        var result = Result.Failure("No code");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("No code");
        result.Code.Should().BeNull();
    }

    [Fact]
    public void GenericSuccess_HoldsData()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_SetsErrorWithoutData()
    {
        var result = Result<string>.Failure("bad", "CODE");

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Be("bad");
        result.Code.Should().Be("CODE");
    }
}
