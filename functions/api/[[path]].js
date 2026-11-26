export async function onRequest(context) {
    const incoming = new URL(context.request.url);
    const upstream = new URL(
        incoming.pathname + incoming.search,
        context.env.API_ORIGIN
    );

    // Keep the browser's Origin and Cookie headers intact. The API uses Origin
    // validation for login, refresh and logout, and the browser stores the
    // returned HttpOnly cookie against the Pages origin.
    const upstreamRequest = new Request(upstream, context.request);
    return fetch(upstreamRequest);
}
