using Iris.Shared.Results;

namespace Iris.Desktop.Services;

internal static class DesktopErrorMessageMapper
{
    public static string ToUserMessage(Error error)
    {
        return error.Code switch
        {
            "chat.message_empty" => "Type a message first.",
            "chat.message_too_long" => "This message is too long.",
            "chat.history_load_failed" => "I could not load the conversation history.",
            "chat.conversation_load_failed" => "I could not load this conversation.",
            "chat.conversation_not_found" => "This conversation could not be found.",
            "chat.message_save_failed" or "chat.commit_failed" => "I could not save the conversation.",
            "model.empty_response" or "model_gateway.provider_empty_response" => "The local model returned an empty response.",
            "model_gateway.provider_unavailable" => "I could not reach Ollama. Check that Ollama is running.",
            "model_gateway.provider_timeout" => "The local model took too long to respond.",
            "model_gateway.provider_not_found" => "The configured Ollama model or endpoint was not found.",
            "model_gateway.provider_rate_limited" => "The local model is temporarily rate limited.",
            "model_gateway.provider_failure" or "model_gateway.provider_http_error" => "The local model returned an error.",
            "model_gateway.provider_invalid_response" or "model_gateway.provider_invalid_role" => "The local model returned a response I could not read.",
            "model_gateway.ollama.base_url_required" or
            "model_gateway.ollama.base_url_invalid" or
            "model_gateway.ollama.base_url_scheme_invalid" or
            "model_gateway.ollama.model_required" or
            "model_gateway.ollama.timeout_invalid" or
            "model_gateway.request.required" or
            "model_gateway.request.empty_messages" or
            "model_gateway.request.temperature_invalid" or
            "model_gateway.request.empty_message_content" or
            "model_gateway.request.role_invalid" => "The local model configuration is incomplete.",
            _ => "I could not send the message."
        };
    }
}
