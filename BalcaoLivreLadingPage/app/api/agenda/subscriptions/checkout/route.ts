import {
  agendaSubscriptionErrorResponse,
  agendaSubscriptionOptionsResponse,
  createPublicAgendaSubscriptionCheckout,
} from "../../../../lib/agenda-subscription-server";

async function checkout(request: Request) {
  try {
    return await createPublicAgendaSubscriptionCheckout(request);
  } catch (error) {
    return agendaSubscriptionErrorResponse(error);
  }
}

export const GET = checkout;
export const POST = checkout;

export async function OPTIONS() {
  return agendaSubscriptionOptionsResponse("GET, POST, OPTIONS");
}
