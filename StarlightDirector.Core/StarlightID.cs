﻿using System;

namespace StarlightDirector.Core {
    public static class StarlightID {

        public static Guid GetGuidFromInt32(int id) {
            return new Guid(id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

    }
}