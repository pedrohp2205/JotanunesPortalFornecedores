using AwesomeAssertions;

using Jotanunes.External.BddTests.Support.Contexts;

namespace Jotanunes.External.BddTests.StepDefinitions.Shared;

[Binding]
public class ExceptionStepDefinitions(ExceptionContext ctx)
{
    [Then(@"deve ser apresentada a mensagem de erro ""(.*)""")]
    [Then(@"a mensagem ""(.*)"" deve ser exibida")]
    public void EntaoDeveSerApresentadaAMensagemDeErro(string mensagemErro)
    {
        ctx.ThrownException.Should().NotBeNull();
        ctx.ThrownException!.Message.Should().Be(mensagemErro);
    }
}
