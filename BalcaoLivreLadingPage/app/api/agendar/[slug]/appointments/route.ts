import {
  agendaErrorResponse,
  createAppointment,
} from "../../../../lib/agenda-booking-server";

type RouteContext = { params: Promise<{ slug: string }> };

export async function POST(request: Request, context: RouteContext) {
  try {
    const { slug } = await context.params;
    return await createAppointment(request, slug);
  } catch (error) {
    return agendaErrorResponse(error);
  }
}
