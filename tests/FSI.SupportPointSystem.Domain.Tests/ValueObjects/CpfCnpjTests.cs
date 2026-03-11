using FluentAssertions;
using FSI.SupportPointSystem.Domain.Exceptions;
using FSI.SupportPointSystem.Domain.ValueObjects;
using Xunit;

namespace FSI.SupportPointSystem.Domain.Tests.ValueObjects;

public sealed class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")] // formato com máscara
    [InlineData("52998224725")]    // apenas dígitos
    public void Create_ValidCpf_ShouldSucceed(string raw)
    {
        var cpf = Cpf.Create(raw);
        cpf.Value.Should().Be("52998224725");
    }

    [Theory]
    [InlineData("111.111.111-11")] // dígitos todos iguais
    [InlineData("000.000.000-00")]
    [InlineData("123.456.789-00")] // dígito verificador errado
    [InlineData("123")]            // tamanho inválido
    public void Create_InvalidCpf_ShouldThrow(string raw)
    {
        var act = () => Cpf.Create(raw);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Formatted_ShouldReturnMaskedCpf()
    {
        var cpf = Cpf.Create("52998224725");
        cpf.Formatted.Should().Be("529.982.247-25");
    }

    [Fact]
    public void Equality_SameCpf_ShouldBeEqual()
    {
        var a = Cpf.Create("52998224725");
        var b = Cpf.Create("529.982.247-25");
        a.Should().Be(b);
    }
}

public sealed class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Create_ValidCnpj_ShouldSucceed(string raw)
    {
        var cnpj = Cnpj.Create(raw);
        cnpj.Value.Should().Be("11222333000181");
    }

    [Theory]
    [InlineData("11.111.111/1111-11")] // repetição
    [InlineData("12.345.678/0001-00")] // dígito errado
    [InlineData("123")]
    public void Create_InvalidCnpj_ShouldThrow(string raw)
    {
        var act = () => Cnpj.Create(raw);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Formatted_ShouldReturnMaskedCnpj()
    {
        var cnpj = Cnpj.Create("11222333000181");
        cnpj.Formatted.Should().Be("11.222.333/0001-81");
    }
}
