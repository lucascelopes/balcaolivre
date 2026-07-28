import {
  agendaErrorResponse,
  patchInternalBooking,
} from "../../../../../lib/agenda-booking-server";

type RouteContext = { params: Promise<{ id: string }> };

export async function PATCH(request: Request, context: RouteContext) {
  try {
    const { id } = await context.params;
    return await patchInternalBooking(request, id);
  } catch (error) {
    return agendaErrorResponse(error);
  }
}
