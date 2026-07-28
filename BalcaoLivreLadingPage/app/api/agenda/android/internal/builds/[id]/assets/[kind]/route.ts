import {
  agendaAndroidErrorResponse,
  getInternalAgendaAndroidBuildAsset,
} from "../../../../../../../../lib/agenda-android-server";

type RouteContext = { params: Promise<{ id: string; kind: string }> };

export async function GET(request: Request, context: RouteContext) {
  try {
    const { id, kind } = await context.params;
    return await getInternalAgendaAndroidBuildAsset(request, id, kind);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}
