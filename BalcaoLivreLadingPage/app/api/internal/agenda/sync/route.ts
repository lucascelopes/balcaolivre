import {
  agendaErrorResponse,
  syncAgenda,
} from "../../../../lib/agenda-booking-server";

export async function POST(request: Request) {
  try {
    return await syncAgenda(request);
  } catch (error) {
    return agendaErrorResponse(error);
  }
}
