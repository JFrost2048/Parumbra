using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAIChatProvider {
    Task<string> GenerateAsync(
        List<(string role, string content)> messages,
        AIConfig config // model/provider/temperature 등
    );
}
